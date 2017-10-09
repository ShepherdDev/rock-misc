using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Reporting;
using Rock.Web.Cache;

namespace com.shepherdchurch.Misc.DataTransform
{
    /// <summary>
    /// Include Spouse Transformation
    /// </summary>
    [Description( "Transform result to include Spouse" )]
    [Export( typeof( DataTransformComponent ) )]
    [ExportMetadata( "ComponentName", "Person Include Spouse Transformation" )]
    public class IncludeSpouseTransform : DataTransformComponent<Rock.Model.Person>
    {
        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        public override string Title
        {
            get { return "Include Spouse"; }
        }

        /// <summary>
        /// Gets the name of the transformed entity type.
        /// </summary>
        /// <value>
        /// The name of the transformed entity type.
        /// </value>
        public override string TransformedEntityTypeName
        {
            get { return "Rock.Model.Person"; }
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        /// <param name="serviceInstance">The service instance.</param>
        /// <param name="parameterExpression">The parameter expression.</param>
        /// <param name="whereExpression">The where expression.</param>
        /// <returns></returns>
        public override Expression GetExpression( IService serviceInstance, ParameterExpression parameterExpression, Expression whereExpression )
        {
            IQueryable<int> idQuery = serviceInstance.GetIds( parameterExpression, whereExpression );
            return BuildExpression( serviceInstance, idQuery, parameterExpression );
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        /// <param name="serviceInstance">The service instance.</param>
        /// <param name="personQueryable">The person queryable.</param>
        /// <param name="parameterExpression">The parameter expression.</param>
        /// <returns></returns>
        public override Expression GetExpression( IService serviceInstance, IQueryable<Rock.Model.Person> personQueryable, ParameterExpression parameterExpression )
        {
            return BuildExpression( serviceInstance, personQueryable.Select( p => p.Id ), parameterExpression );
        }

        /// <summary>
        /// Builds the expression.
        /// </summary>
        /// <param name="serviceInstance">The service instance.</param>
        /// <param name="idQuery">The id query.</param>
        /// <param name="parameterExpression">The parameter expression.</param>
        /// <returns></returns>
        private Expression BuildExpression( IService serviceInstance, IQueryable<int> idQuery, ParameterExpression parameterExpression )
        {
            var rockContext = ( RockContext ) serviceInstance.Context;
            var adultGuid = Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid();
            var adultGroupRoleId = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY ).Roles.FirstOrDefault( r => r.Guid == adultGuid ).Id;
            var marriedStatusId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_MARITAL_STATUS_MARRIED ).Id;
            var familyGroupTypeId = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY ).Id;

            //
            // This duplicates the functionality of the ufnCrm_GetSpousePersonIdFromPersonId function
            // on a broader level. We are given a list of Person IDs so we need to handle more than one
            // at a time. That is what the GroupBy method does at the end. We group by the original person
            // Id and then order within that group to find the "first" spouse.
            //
            var sQuery = new GroupService( rockContext ).Queryable()
                .Join( rockContext.GroupMembers, f => f.Id, fm1 => fm1.GroupId, ( f, fm1 ) => new { f, fm1 } )
                .Join( rockContext.People, x => x.fm1.PersonId, p1 => p1.Id, ( x, p1 ) => new { x.f, x.fm1, p1 } )
                .Join( rockContext.GroupMembers, x => x.f.Id, fm2 => fm2.GroupId, ( x, fm2 ) => new { x.f, x.fm1, x.p1, fm2 } )
                .Join( rockContext.People, x => x.fm2.PersonId, p2 => p2.Id, ( x, p2 ) => new { x.f, x.fm1, x.p1, x.fm2, p2 } )
                .Where( x => x.f.GroupTypeId == familyGroupTypeId &&
                        idQuery.Contains( x.p1.Id ) &&
                        x.fm1.GroupRoleId == adultGroupRoleId &&
                        x.fm2.GroupRoleId == adultGroupRoleId &&
                        x.p1.MaritalStatusValueId == marriedStatusId &&
                        x.p2.MaritalStatusValueId == marriedStatusId &&
                        x.p1.Id != x.p2.Id &&
                        ( x.p1.Gender != x.p2.Gender || x.p1.Gender == Gender.Unknown || x.p2.Gender == Gender.Unknown ) )
                .Select( x => new
                {
                    p1 = x.p1,
                    p2 = x.p2,
                    dif = Math.Abs( DbFunctions.DiffDays( x.p1.BirthDate ?? new DateTime( 1, 1, 1 ), x.p2.BirthDate ?? new DateTime( 1, 1, 1 ) ).Value )
                } )
                .GroupBy( x => x.p1 )
                .Select( x => x.OrderBy( y => y.dif ).ThenBy( y => y.p2.Id ).FirstOrDefault().p2.Id );

            //
            // Finaly build a query that gives us all people who are either the original person or
            // included in the spouse query.
            //
            var query = new PersonService( rockContext ).Queryable()
                .Where( p => sQuery.Contains( p.Id ) || idQuery.Contains( p.Id ) );

            return FilterExpressionExtractor.Extract<Rock.Model.Person>( query, parameterExpression, "p" );
        }
    }
}