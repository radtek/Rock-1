﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;

using Quartz;

using Rock.Attribute;
using Rock.Data;
using Rock.Logging;
using Rock.Model;

namespace Rock.Jobs
{

    /// <summary>
    /// Job to process communications
    /// </summary>
    [DisplayName( "Send Communications" )]
    [Description( "Job to send any future communications or communications not sent immediately by Rock." )]

    [IntegerField( "Delay Period", "The number of minutes to wait before sending any new communication (If the communication block's 'Send When Approved' option is turned on, then a delay should be used here to prevent a send overlap).", false, 30, "", 0 )]
    [IntegerField( "Expiration Period", "The number of days after a communication was created or scheduled to be sent when it should no longer be sent.", false, 3, "", 1 )]
    [DisallowConcurrentExecution]
    public class SendCommunications : IJob
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendCommunications"/> class.
        /// </summary>
        public SendCommunications()
        {
        }

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public virtual void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            int expirationDays = dataMap.GetInt( "ExpirationPeriod" );
            int delayMinutes = dataMap.GetInt( "DelayPeriod" );

            IOrderedEnumerable<Model.Communication> sendCommunications = null;
            var stopWatch = Stopwatch.StartNew();
            using ( var rockContext = new RockContext() )
            {
                sendCommunications = new CommunicationService( rockContext )
                    .GetQueued( expirationDays, delayMinutes, false, false )
                    .AsNoTracking()
                    .ToList()
                    .OrderBy( c => c.Id );
            }

            RockLogger.Log.Information( RockLogDomains.Jobs, "{0}: Queued communication query runtime: {1} ms", nameof(SendCommunications), stopWatch.ElapsedMilliseconds );

            if ( sendCommunications == null )
            {
                context.Result = "No communications to send";
            }

            var exceptionMsgs = new List<string>();
            int communicationsSent = 0;

            stopWatch = Stopwatch.StartNew();
            foreach ( var comm in sendCommunications )
            {
                try
                {
                    Rock.Model.Communication.Send( comm );
                    communicationsSent++;
                }

                catch ( Exception ex )
                {
                    exceptionMsgs.Add( string.Format( "Exception occurred sending communication ID:{0}:{1}    {2}", comm.Id, Environment.NewLine, ex.Messages().AsDelimited( Environment.NewLine + "   " ) ) );
                    ExceptionLogService.LogException( ex, System.Web.HttpContext.Current );
                }
            }
            RockLogger.Log.Information( RockLogDomains.Jobs, "{0}: Send communications runtime: {1} ms", nameof( SendCommunications ), stopWatch.ElapsedMilliseconds );

            if ( communicationsSent > 0 )
            {
                context.Result = string.Format( "Sent {0} {1}", communicationsSent, "communication".PluralizeIf( communicationsSent > 1 ) );
            }
            else
            {
                context.Result = "No communications to send";
            }

            if ( exceptionMsgs.Any() )
            {
                throw new Exception( "One or more exceptions occurred sending communications..." + Environment.NewLine + exceptionMsgs.AsDelimited( Environment.NewLine ) );
            }

            // check for communications that have not been sent but are past the expire date. Mark them as failed and set a warning.
            var expireDateTimeEndWindow = RockDateTime.Now.AddDays( 0 - expirationDays );

            // limit the query to only look a week prior to the window to avoid performance issue (it could be slow to query at ALL the communication recipient before the expire date, as there could several years worth )
            var expireDateTimeBeginWindow = expireDateTimeEndWindow.AddDays( -7 );

            stopWatch = Stopwatch.StartNew();
            using ( var rockContext = new RockContext() )
            {
                var qryExpiredRecipients = new CommunicationRecipientService( rockContext ).Queryable()
                    .Where( cr =>
                        cr.Communication.Status == CommunicationStatus.Approved &&
                        cr.Status == CommunicationRecipientStatus.Pending &&
                        (
                            ( !cr.Communication.FutureSendDateTime.HasValue && cr.Communication.ReviewedDateTime.HasValue && cr.Communication.ReviewedDateTime < expireDateTimeEndWindow && cr.Communication.ReviewedDateTime > expireDateTimeBeginWindow )
                            || ( cr.Communication.FutureSendDateTime.HasValue && cr.Communication.FutureSendDateTime < expireDateTimeEndWindow && cr.Communication.FutureSendDateTime > expireDateTimeBeginWindow )
                        ) );

                rockContext.BulkUpdate( qryExpiredRecipients, c => new CommunicationRecipient { Status = CommunicationRecipientStatus.Failed, StatusNote = "Communication was not sent before the expire window (possibly due to delayed approval)." } );
            }
            RockLogger.Log.Information( RockLogDomains.Jobs, "{0}: Mark failed communications runtime: {1} ms", nameof( SendCommunications ), stopWatch.ElapsedMilliseconds );
        }
    }
}