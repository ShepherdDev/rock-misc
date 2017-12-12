using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Http;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;

namespace com.shepherdchurch.Misc.Rest
{
    public class SC_Misc_BlocksController : ApiControllerBase
    {
        #region API Methods

        /// <summary>
        /// Retrieve the HTML from the SQL query and Lava Template in the block.
        /// </summary>
        /// <param name="blockGuid">The GUID that identifies the block to run this query for.</param>
        /// <returns>An HTML formatted string.</returns>
        [HttpGet]
        [System.Web.Http.Route( "api/SC_Misc_Blocks/GetHtmlForSqlBlock/{blockGuid}" )]
        public string GetHtmlForSqlBlock( Guid blockGuid )
        {
            RockContext rockContext = new RockContext();
            Block block = new BlockService( rockContext ).Get( blockGuid );

            if ( block != null )
            {
                block.LoadAttributes();

                string lavaTemplate = block.GetAttributeValue( "LavaTemplate" );
                string sql = block.GetAttributeValue( "Sql" );

                var results = DbService.GetDataSet( sql.ToString(), CommandType.Text, null, null );

                var dropRows = new List<DataRowDrop>();
                if ( results.Tables.Count == 1 )
                {
                    foreach ( DataRow row in results.Tables[0].Rows )
                    {
                        dropRows.Add( new DataRowDrop( row ) );
                    }
                }

                Dictionary<string, object> mergeValues = new Dictionary<string, object>();
                mergeValues.Add( "Rows", dropRows );

                return lavaTemplate.ResolveMergeFields( mergeValues );
            }

            return string.Format(
                @"<div class='alert alert-danger'> 
                    unable to find block_guid: {0}
                </div>",
                blockGuid );
        }

        /// <summary>
        /// Retrieve the JS from the SQL query and Lava Template in the block.
        /// </summary>
        /// <param name="blockGuid">The GUID that identifies the block to run this query for.</param>
        /// <returns>An HTML formatted string.</returns>
        [HttpGet]
        [System.Web.Http.Route( "api/SC_Misc_Blocks/GetJavascriptForSqlBlock/{blockGuid}" )]
        public IEnumerable<Dictionary<string, object>> GetJavascriptForSqlBlock( Guid blockGuid )
        {
            RockContext rockContext = new RockContext();
            Block block = new BlockService( rockContext ).Get( blockGuid );

            if ( block != null )
            {
                block.LoadAttributes();

                string sql = block.GetAttributeValue( "Sql" );

                var parameters = ActionContext.Request.GetQueryStrings()
                    .AsQueryable()
                    .ToDictionary( x => x.Key, x => ( object ) x.Value );

                var results = DbService.GetDataSet( sql.ToString(), CommandType.Text, parameters, null );

                var rows = new List<Dictionary<string, object>>();
                foreach ( DataRow row in results.Tables[0].Rows )
                {
                    var r = new Dictionary<string, object>();

                    foreach ( DataColumn column in results.Tables[0].Columns )
                    {
                        r.Add( column.ColumnName, row[column] );
                    }

                    rows.Add( r );
                }

                return rows;
            }

            return null;
        }

        #endregion

        #region Classes

        /// <summary>
        ///
        /// </summary>
        private class DataRowDrop : DotLiquid.Drop
        {
            private readonly DataRow _dataRow;

            public DataRowDrop( DataRow dataRow )
            {
                _dataRow = dataRow;
            }

            public override object BeforeMethod( string method )
            {
                if ( _dataRow.Table.Columns.Contains( method ) )
                {
                    return _dataRow[method];
                }

                return null;
            }
        }

        #endregion
    }
}
