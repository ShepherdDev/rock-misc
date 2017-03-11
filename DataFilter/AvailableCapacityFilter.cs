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
using System.ComponentModel.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Reporting;
using Rock.Web.UI.Controls;

namespace com_shepherdchurch.Misc.DataFilter
{
    /// <summary>
    /// Filter groups based on the available capacity in the group
    /// </summary>
    [Description( "Filter groups based on the available capacity in the group" )]
    [Export( typeof( DataFilterComponent ) )]
    [ExportMetadata( "ComponentName", "Available Capacity" )]
    public class AvailableCapacityFilter : DataFilterComponent
    {
        #region Properties

        /// <summary>
        /// Gets the entity type that filter applies to.
        /// </summary>
        /// <value>
        /// The entity that filter applies to.
        /// </value>
        public override string AppliesToEntityType
        {
            get { return typeof( Rock.Model.Group ).FullName; }
        }

        /// <summary>
        /// Gets the section.
        /// </summary>
        /// <value>
        /// The section.
        /// </value>
        public override string Section
        {
            get { return "com_shepherdchurch"; }// return "Additional Filters"; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <value>
        /// The title.
        /// </value>
        public override string GetTitle( Type entityType )
        {
            return "Available Capacity";
        }

        /// <summary>
        /// Formats the selection on the client-side.  When the filter is collapsed by the user, the Filterfield control
        /// will set the description of the filter to whatever is returned by this property.  If including script, the
        /// controls parent container can be referenced through a '$content' variable that is set by the control before 
        /// referencing this property.
        /// </summary>
        /// <value>
        /// The client format script.
        /// </value>
        public override string GetClientFormatSelection( Type entityType )
        {
            return @"
function () {
    var result = 'Available Capacity';

    result += ' ' + $('.js-filter-compare', $content).find(':selected').text();
    var countText = $('.js-capacity-count', $content).filter(':visible').length ? $('.js-member-count', $content).filter(':visible').val() : '';
    result += ' ' + countText;
    return result; 
}

";
        }

        /// <summary>
        /// Formats the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override string FormatSelection( Type entityType, string selection )
        {
            var values = selection.Split( '|' );
            string result = "Available Capacity";
            if ( values.Length == 2 )
            {
                ComparisonType comparisonType = values[0].ConvertToEnum<ComparisonType>( ComparisonType.EqualTo );
                int? capacityCountValue = values[1].AsIntegerOrNull();

                string countText = ( comparisonType == ComparisonType.IsBlank || comparisonType == ComparisonType.IsNotBlank ) ? string.Empty : capacityCountValue.ToString();
                result += " " + comparisonType.ConvertToString() + " " + countText;
            }

            return result;
        }

        /// <summary>
        /// Creates the child controls.
        /// </summary>
        /// <returns></returns>
        public override Control[] CreateChildControls( Type entityType, FilterField filterControl )
        {
            var ddlIntegerCompare = ComparisonHelper.ComparisonControl( ComparisonHelper.NumericFilterComparisonTypes );
            ddlIntegerCompare.Label = "Count";
            ddlIntegerCompare.ID = string.Format( "{0}_ddlIntegerCompare", filterControl.ID );
            ddlIntegerCompare.AddCssClass( "js-filter-compare" );
            filterControl.Controls.Add( ddlIntegerCompare );

            var nbCapacityCount = new NumberBox();
            nbCapacityCount.Label = "&nbsp;";
            nbCapacityCount.ID = string.Format( "{0}_nbCapacityCount", filterControl.ID );
            nbCapacityCount.AddCssClass( "js-filter-control js-capacity-count" );
            nbCapacityCount.FieldName = "Capacity Count";
            filterControl.Controls.Add( nbCapacityCount );

            return new Control[] { ddlIntegerCompare, nbCapacityCount };
        }

        /// <summary>
        /// Renders the controls.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="filterControl">The filter control.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="controls">The controls.</param>
        public override void RenderControls( Type entityType, FilterField filterControl, HtmlTextWriter writer, Control[] controls )
        {
            DropDownList ddlCompare = controls[0] as DropDownList;
            NumberBox nbValue = controls[1] as NumberBox;

            // Comparison Row
            writer.AddAttribute( "class", "row field-criteria" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );

            // Comparison Type
            writer.AddAttribute( "class", "col-md-4" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            ddlCompare.RenderControl( writer );
            writer.RenderEndTag();

            ComparisonType comparisonType = ( ComparisonType ) ( ddlCompare.SelectedValue.AsInteger() );
            nbValue.Style[HtmlTextWriterStyle.Display] = ( comparisonType == ComparisonType.IsBlank || comparisonType == ComparisonType.IsNotBlank ) ? "none" : string.Empty;

            // Comparison Value
            writer.AddAttribute( "class", "col-md-8" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            nbValue.RenderControl( writer );
            writer.RenderEndTag();

            writer.RenderEndTag();  // row

            RegisterFilterCompareChangeScript( filterControl );
        }

        /// <summary>
        /// Gets the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="controls">The controls.</param>
        /// <returns></returns>
        public override string GetSelection( Type entityType, Control[] controls )
        {
            DropDownList ddlCompare = controls[0] as DropDownList;
            NumberBox nbValue = controls[1] as NumberBox;

            return string.Format( "{0}|{1}", ddlCompare.SelectedValue, nbValue.Text );
        }

        /// <summary>
        /// Sets the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="controls">The controls.</param>
        /// <param name="selection">The selection.</param>
        public override void SetSelection( Type entityType, Control[] controls, string selection )
        {
            var values = selection.Split( '|' );

            DropDownList ddlCompare = controls[0] as DropDownList;
            NumberBox nbValue = controls[1] as NumberBox;

            if ( values.Length >= 2 )
            {
                ComparisonType comparisonType = ( ComparisonType ) ( values[0].AsInteger() );
                ddlCompare.SelectedValue = comparisonType.ConvertToInt().ToString();
                nbValue.Text = values[1];
            }
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="serviceInstance">The service instance.</param>
        /// <param name="parameterExpression">The parameter expression.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override Expression GetExpression( Type entityType, IService serviceInstance, ParameterExpression parameterExpression, string selection )
        {
            var values = selection.Split( '|' );

            ComparisonType comparisonType = values[0].ConvertToEnum<ComparisonType>( ComparisonType.EqualTo );
            int? capacityCountValue = values[1].AsIntegerOrNull();

            var query = new GroupService( ( RockContext ) serviceInstance.Context ).Queryable();
            var capacityCountEqualQuery = query
                .Where( p =>
                    ( ( p.GroupCapacity.HasValue ? p.GroupCapacity.Value : int.MaxValue ) - p.Members.Count( a => a.GroupMemberStatus == GroupMemberStatus.Active ) )
                    == capacityCountValue );
            var capacityRuleQuery = query.Where( p => p.GroupType.GroupCapacityRule != GroupCapacityRule.Hard );

            BinaryExpression compareEqualExpression = FilterExpressionExtractor.Extract<Rock.Model.Group>( capacityCountEqualQuery, parameterExpression, "p" ) as BinaryExpression;
            BinaryExpression capacityRuleExpression = FilterExpressionExtractor.Extract<Rock.Model.Group>( capacityRuleQuery, parameterExpression, "p" ) as BinaryExpression;
            BinaryExpression compareResultExpression = FilterExpressionExtractor.AlterComparisonType( comparisonType, compareEqualExpression, 0 );

            BinaryExpression result = Expression.MakeBinary( ExpressionType.Or, compareResultExpression, capacityRuleExpression );

            return result;
        }

        #endregion
    }
}