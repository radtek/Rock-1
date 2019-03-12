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
using System.Linq;
using System.Linq.Expressions;

namespace Rock.Data
{
    /// <summary>
    /// Interface for all <see cref="Service&lt;T&gt;"/> classes
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Gets the parameter expression.
        /// </summary>
        /// <value>
        /// The parameter expression.
        /// </value>
        ParameterExpression ParameterExpression { get; }

        /// <summary>
        /// Gets the context.
        /// </summary>
        /// <value>
        /// The context.
        /// </value>
        DbContext Context { get; }

        /// <summary>
        /// Gets the ids.
        /// </summary>
        /// <param name="parameterExpression">The parameter expression.</param>
        /// <param name="whereExpression">The where expression.</param>
        /// <returns></returns>
        IQueryable<int> GetIds( ParameterExpression parameterExpression, Expression whereExpression );

        /// <summary>
        /// Gets the IEntity with the id value
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        IEntity Get( int id );

        /// <summary>
        /// Gets the IEntity with the id value but doesn't load it into the EF ChangeTracker.
        /// Use this if you won't be making any changes to the record
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        IEntity GetNoTracking( int id );

        /// <summary>
        /// Gets the IEntity with the id value
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        IEntity Get( Guid guid );

        /// <summary>
        /// Gets the IEntity with the id value but doesn't load it into the EF ChangeTracker.
        /// Use this if you won't be making any changes to the record
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        IEntity GetNoTracking( Guid guid );
    }
}
