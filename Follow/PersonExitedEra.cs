using System;
using System.ComponentModel;
using System.ComponentModel.Composition;

using Rock;
using Rock.Data;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Follow;

namespace com.shepherdchurch.Misc.Follow
{
    /// <summary>
    /// Person Entered eRA
    /// </summary>
    [Description( "Person Exited eRA" )]
    [Export( typeof( EventComponent ) )]
    [ExportMetadata( "ComponentName", "PersonExitedEra" )]

    [IntegerField( "Max Days Back", "Maximum number of days back to consider", false, 30, "", order: 0 )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "Currently an eRA Attribute", "The attribute that contains the boolean value indicating if the person is currently an eRA.", true, false, Rock.SystemGuid.Attribute.PERSON_ERA_CURRENTLY_AN_ERA, order: 1 )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "eRA End Date Attribute", "The attribute that contains the date the person exited eRA status.", true, false, Rock.SystemGuid.Attribute.PERSON_ERA_END_DATE, order: 2 )]
    public class PersonExitedEra : EventComponent
    {
        #region Event Component Implementation

        /// <summary>
        /// Gets the followed entity type identifier.
        /// </summary>
        /// <value>
        /// The followed entity type identifier.
        /// </value>
        public override Type FollowedType
        {
            get { return typeof( PersonAlias ); }
        }

        /// <summary>
        /// Determines whether the event has happened for the entity.
        /// </summary>
        /// <param name="followingEvent">The following event.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="lastNotified">The last notified.</param>
        /// <returns></returns>
        public override bool HasEventHappened( FollowingEventType followingEvent, IEntity entity, DateTime? lastNotified )
        {
            if ( lastNotified.HasValue )
            {
                return false;
            }

            var currentlyAnEraAttribute = AttributeCache.Get( GetAttributeValue( followingEvent, "CurrentlyaneRAAttribute" ).AsGuid() );
            var eraEndDateAttribute = AttributeCache.Get( GetAttributeValue( followingEvent, "eRAEndDateAttribute" ).AsGuid() );

            if ( followingEvent != null && entity != null && currentlyAnEraAttribute != null && eraEndDateAttribute != null )
            {
                var personAlias = entity as PersonAlias;
                if ( personAlias != null && personAlias.Person != null )
                {
                    var person = personAlias.Person;

                    if ( person.Attributes == null )
                    {
                        person.LoadAttributes();
                    }

                    bool currentlyAnEra = person.GetAttributeValue( currentlyAnEraAttribute.Key ).AsBoolean( false );
                    DateTime? date = person.GetAttributeValue( eraEndDateAttribute.Key ).AsDateTime();

                    if ( !currentlyAnEra && date.HasValue )
                    {
                        int daysBack = GetAttributeValue( followingEvent, "MaxDaysBack" ).AsInteger();
                        var processDate = RockDateTime.Today;
                        if ( !followingEvent.SendOnWeekends && RockDateTime.Today.DayOfWeek == DayOfWeek.Friday )
                        {
                            daysBack += 2;
                        }

                        if ( processDate.Subtract( date.Value ).Days < daysBack )
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        #endregion

    }
}