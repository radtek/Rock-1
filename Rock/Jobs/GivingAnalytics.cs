// <copyright>
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
using System.ComponentModel;
using System.Linq;
using System.Text;
using Quartz;
using Rock.Attribute;
using Rock.SystemKey;
using Rock.Utility.Settings.GivingAnalytics;

namespace Rock.Jobs
{
    /// <summary>
    /// Job that serves two purposes:
    ///   1.) Update Classification Attributes. This will be done no more than once a day and only on the days of week
    ///       configured in the analytics settings.
    ///   2.) Send Alerts - Sends alerts for gifts since the last run date and determines ‘Follow-up Alerts’ (alerts
    ///       triggered from gifts expected but not given) once a day.
    /// </summary>
    [DisplayName( "Giving Analytics" )]
    [Description( "Job that updates giving classification attributes as well as sending giving alerts." )]
    [DisallowConcurrentExecution]

    [IntegerField( "Classification Lifespan",
        Description = "The number of days that a giving classification is valid before this job will re-classify the giving group.",
        DefaultIntegerValue = AttributeDefaultValue.ClassificationLifespanDays,
        Key = AttributeKey.ClassificationLifespanDays,
        Order = 1 )]

    [IntegerField( "Max Days Since Last Gift",
        Description = "The maximum number of days since a giving group last gave where classification will be made. If the last gift was earlier than this maximum, then classification is probably not relevant.",
        DefaultIntegerValue = AttributeDefaultValue.MaxDaysSinceLastGift,
        Key = AttributeKey.MaxDaysSinceLastGift,
        Order = 2 )]

    public class GivingAnalytics : IJob
    {
        #region Keys

        /// <summary>
        /// Attribute Keys
        /// </summary>
        private static class AttributeKey {
            public const string ClassificationLifespanDays = "ClassificationLifespanDays";
            public const string MaxDaysSinceLastGift = "MaxDaysSinceLastGift";
        }

        /// <summary>
        /// Default Values for Attributes
        /// </summary>
        private static class AttributeDefaultValue {
            public const int ClassificationLifespanDays = 45;
            public const int MaxDaysSinceLastGift = 548;
        }

        #endregion Keys

        #region Constructors

        /// <summary>
        /// Empty constructor for job initialization
        /// <para>
        /// Jobs require a public empty constructor so that the
        /// scheduler can instantiate the class whenever it needs.
        /// </para>
        /// </summary>
        public GivingAnalytics()
        {
        }

        #endregion Constructors

        #region Execute

        /// <summary>
        /// Job to get a National Change of Address (NCOA) report for all active people's addresses.
        ///
        /// Called by the <see cref="IScheduler" /> when a
        /// <see cref="ITrigger" /> fires that is associated with
        /// the <see cref="IJob" />.
        /// </summary>
        public virtual void Execute( IJobExecutionContext jobContext )
        {
            var context = new GivingAnalyticsContext( jobContext );
            UpdateClassificationAttributes( context );



            // Build results
            var results = new StringBuilder();

            // Format the result message
            results.AppendLine( $"Opened {updatedLocationCount} {"location".PluralizeIf( updatedLocationCount != 1 )}" );

            context.Result = results.ToString();

            if ( errors.Any() )
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine();
                sb.Append( "Errors: " );
                errors.ForEach( e => { sb.AppendLine(); sb.Append( e ); } );
                string errorMessage = sb.ToString();
                context.Result += errorMessage;
                // We're not going to throw an aggregate exception unless there were no successes.
                // Otherwise the status message does not show any of the success messages in
                // the last status message.
                if ( updatedLocationCount == 0 )
                {
                    throw new AggregateException( exceptions.ToArray() );
                }
            }
        }

        #endregion Execute

        #region Settings and Attribute Helpers

        /// <summary>
        /// Gets the last run date time.
        /// </summary>
        /// <returns></returns>
        private DateTime? GetLastRunDateTime() {
            var settings = Rock.Web.SystemSettings
                .GetValue( SystemSetting.GIVING_ANALYTICS_CONFIGURATION )
                .FromJsonOrNull<GivingAnalyticsSetting>();

            return settings?.GivingAnalytics.GivingAnalyticsLastRunDateTime;
        }

        /// <summary>
        /// Gets the earliest valid classification date time. Classifications made before this time are
        /// no longer valid and should be re-classified.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private DateTime GetEarliestValidClassificationDateTime( GivingAnalyticsContext context )
        {
            var days = context.GetAttributeValue( AttributeKey.ClassificationLifespanDays ).AsIntegerOrNull() ??
                AttributeDefaultValue.ClassificationLifespanDays;
            return context.Now.AddDays( 0 - days );
        }

        /// <summary>
        /// Gets the earliest last gift date time.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private DateTime GetEarliestLastGiftDateTime( GivingAnalyticsContext context )
        {
            var days = context.GetAttributeValue( AttributeKey.MaxDaysSinceLastGift ).AsIntegerOrNull() ??
                AttributeDefaultValue.MaxDaysSinceLastGift;
            return context.Now.AddDays( 0 - days );
        }

        #region Settings and Attribute Helpers

        /// <summary>
        /// Updates the classification attributes.
        /// </summary>
        private void UpdateClassificationAttributes( GivingAnalyticsContext context )
        {
            // Classification attributes need to be written for all adults with the same giver id in Rock. So Ted &
            // Cindy should have the same attribute values if they are set to contribute as a family even if Cindy
            // is always the one giving the gift.

            // Get list of gifts since the last run AND for individuals where the LastClassifiedRunDateTime person
            // attribute is over X days where X is defined as a job setting (defaulted to 45 days) and the LastGiftDate
            // attribute is within Y days where Y is a job setting (defaulted to 548 days). Y helps us to give up on a
            // giver who has stopped giving after a default period of a year and a half.
            var earliestValidClassificationDateTime = GetEarliestValidClassificationDateTime( context );
            var earliestLastGiftDateTime = GetEarliestLastGiftDateTime( context );
        }
    }

    /// <summary>
    /// Giving Analytics Context
    /// </summary>
    internal sealed class GivingAnalyticsContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GivingAnalyticsContext"/> class.
        /// </summary>
        /// <param name="jobExecutionContext">The job execution context.</param>
        public GivingAnalyticsContext( IJobExecutionContext jobExecutionContext )
        {
            JobExecutionContext = jobExecutionContext;
            JobDataMap = jobExecutionContext.JobDetail.JobDataMap;
        }

        /// <summary>
        /// The date time to consider as current time. The time when this processing instance began
        /// </summary>
        public readonly DateTime Now = RockDateTime.Now;

        /// <summary>
        /// Gets the job execution context.
        /// </summary>
        /// <value>
        /// The job execution context.
        /// </value>
        public IJobExecutionContext JobExecutionContext { get; }

        /// <summary>
        /// Gets the job data map.
        /// </summary>
        /// <value>
        /// The job data map.
        /// </value>
        public JobDataMap JobDataMap { get; }

        /// <summary>
        /// Gets the attribute value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public string GetAttributeValue( string key )
        {
            return JobDataMap.GetString( key );
        }
    }
}

/*


1. Get list of gifts since the last run AND for individuals where the LastClassifiedRunDateTime person attribute is over X days where X is defined as a job setting (defaulted to 45 days) and the LastGiftDate attribute is with in Y days where Y is a job setting (defaulted to 548 days). Y helps us to give up on a giver who has stopped giving after a default period of a year and a half.

2. Classifications will be based on the following logic:

A.) First determine the ranges for each of the 4 giving bins by looking at all contribution transactions in the last 12 months. These ranges will be updated in the Giving Analytics system settings.

B.) Get a list of all giving units (distinct giver ids) that have given since the last classification (see rules in #1 above).

C.) For each giving unit:
i.) Classifications for: % Scheduled, Gives As ___, Preferred Source, Preferred Currency
Will be based off of all giving in the last 12 months. In the case of a tie in values (e.g. 50% credit card, 50% cash) use the most recent 
 value as the tie breaker. This could be calculated with only one gift.

ii.) Classifications for: Bin, Percentile
a.) If there is 12 months of giving use that.
b.) If not then use the current number of days of gifts to extrapolate a full year. So if you have 60 days of giving, multiply the giving 
     amount by 6.08 (356 / 60). But there must be at least 3 gifts.

iii.) Classification for: Median Amount, IQR Amount, Mean Frequency, Frequency Standard Deviation
a.) If there is 12 months of giving use all of those
b.) If not use the previous gifts that are within 12 months but there must be at least 5 gifts.
c.) For Amount: we will calulate the median and interquartile range
d.) For Frequency: we will calculate the trimmed mean and standard deviation. The trimmed mean will exlcude the top 10% 
   largest and smallest gifts with in the dataset. If the number of gifts available is < 10 then we’ll remove the top largest and smallest gift. 
/*
}
}
}

/*
Update Classification Attributes
Classification attributes need to be written for all adults with the same giver id in Rock. So Ted & Cindy should have the same attribute values if they are set to contribute as a family even if Cindy is always the one giving the gift.

1. Get list of gifts since the last run AND for individuals where the LastClassifiedRunDateTime person attribute is over X days where X is defined as a job setting (defaulted to 45 days) and the LastGiftDate attribute is with in Y days where Y is a job setting (defaulted to 548 days). Y helps us to give up on a giver who has stopped giving after a default period of a year and a half.

2. Classifications will be based on the following logic:

A.) First determine the ranges for each of the 4 giving bins by looking at all contribution transactions in the last 12 months. These ranges will be updated in the Giving Analytics system settings.

B.) Get a list of all giving units (distinct giver ids) that have given since the last classification (see rules in #1 above).

C.) For each giving unit:
i.) Classifications for: % Scheduled, Gives As ___, Preferred Source, Preferred Currency
Will be based off of all giving in the last 12 months. In the case of a tie in values (e.g. 50% credit card, 50% cash) use the most recent 
 value as the tie breaker. This could be calculated with only one gift.

ii.) Classifications for: Bin, Percentile
a.) If there is 12 months of giving use that.
b.) If not then use the current number of days of gifts to extrapolate a full year. So if you have 60 days of giving, multiply the giving 
     amount by 6.08 (356 / 60). But there must be at least 3 gifts.

iii.) Classification for: Median Amount, IQR Amount, Mean Frequency, Frequency Standard Deviation
a.) If there is 12 months of giving use all of those
b.) If not use the previous gifts that are within 12 months but there must be at least 5 gifts.
c.) For Amount: we will calulate the median and interquartile range
d.) For Frequency: we will calculate the trimmed mean and standard deviation. The trimmed mean will exlcude the top 10% 
   largest and smallest gifts with in the dataset. If the number of gifts available is < 10 then we’ll remove the top largest and smallest gift. 

Frequency Labels:  
Weekly = Avg days between 4.5 - 8.5; Std Dev < 7;          
2 Weeks = Avg days between 9-17; Std Dev < 10;                
Monthly = Avg days between 25-35; Std Dev < 10;             
Quarterly = Avg days between 80-110; Std Dev < 15

Erratic = Freq Avg / 2 < Std Dev 
Undetermined = Everything else                                         

*/