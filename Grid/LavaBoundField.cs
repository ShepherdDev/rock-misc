﻿using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;

using Rock;
using Rock.Web.UI.Controls;

namespace com_shepherdchurch.Misc.Grid
{
    /// <summary>
    /// Process the field value through Lava and output the results.
    /// </summary>
    class LavaBoundField : RockBoundField
    {
        /// <summary>
        /// The Lava template to process the value with.
        /// </summary>
        public string LavaTemplate { get; set; }

        /// <summary>
        /// The key to use when providing the value to the Lava Template. Defaults to "Item".
        /// </summary>
        public string LavaKey { get; set; }

        /// <summary>
        /// Extra Lava Merge Fields to provide to the Lava Template.
        /// </summary>
        public IDictionary<string, object> LavaMergeFields { get; set; }

        /// <summary>
        /// Formats the specified field value for a cell in the <see cref="T:System.Web.UI.WebControls.BoundField" /> object.
        /// </summary>
        /// <param name="dataValue">The field value to format.</param>
        /// <param name="encode">true to encode the value; otherwise, false.</param>
        /// <returns>
        /// The field value converted to the format specified by <see cref="P:System.Web.UI.WebControls.BoundField.DataFormatString" />.
        /// </returns>
        protected override string FormatDataValue( object dataValue, bool encode )
        {
            IDictionary<string, object> mergeFields;

            if ( LavaMergeFields != null )
            {
                mergeFields = new Dictionary<string, object>( LavaMergeFields );
            }
            else
            {
                mergeFields = new Dictionary<string, object>();
            }

            /* Certain empty values come through as DBNull objects which are not Lava friendly. */
            if ( dataValue != null && dataValue.GetType() == typeof( DBNull ) )
            {
                dataValue = null;
            }

            mergeFields.Add( LavaKey ?? "Item", dataValue );
            dataValue = LavaTemplate.ResolveMergeFields( mergeFields );

            return base.FormatDataValue( dataValue, encode );
        }

        /// <summary>
        /// Gets the value that should be exported to Excel
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public override object GetExportValue( GridViewRow row )
        {
            var dataValue = base.GetExportValue( row );
            return FormatDataValue( dataValue, false );
        }
    }
}
