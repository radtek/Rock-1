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
namespace Rock.Store
{
    /// <summary>
    /// Represents a store category for packages.
    /// </summary>
    public class Organization : StoreModel
    {
        /// <summary>
        /// Gets or sets the key for the organization. 
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the key of the organization.
        /// </value>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the Name of the Organization. 
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the Name of the Organization.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the logo URL.
        /// </summary>
        /// <value>
        /// The logo URL.
        /// </value>
        public string LogoUrl { get; set; }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the city.
        /// </summary>
        /// <value>
        /// The city.
        /// </value>
        public string City { get; set; }

        /// <summary>
        /// Gets or sets the average weekly attendance.
        /// </summary>
        /// <value>
        /// The average weekly attendance.
        /// </value>
        public int AverageWeeklyAttendance { get; set; }
    }
}
