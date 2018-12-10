using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Quartz;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace com.shepherdchurch.Misc.Jobs
{
    [DataViewField( "Engaged DataView", "The DataView to use to determine if somebody is engaged.", entityTypeName: "Rock.Model.Person", order: 0 )]
    [DataViewField( "Disengaged DataView", "The DataView to use to determine if somebody is no longer engaged. If this DataView is not set then anybody who is not in the engaged DataView will be considered as no longer engaged.", false, entityTypeName: "Rock.Model.Person", order: 0 )]
    [WorkflowTypeField( "Entry Workflow", "The workflow type to launch when a person becomes engaged.", order: 2 )]
    [WorkflowTypeField( "Exit Workflow", "The workflow type to launch when a person is no longer engaged.", order: 3 )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "Engaged Attribute", "The Boolean Person attribute to set when a person enters or leaves engagement status.", false, false, order: 4 )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "Start Date Attribute", "The Date Person attribute to set when a person enters engagement status.", false, false, order: 5 )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "End Date Attribute", "The Date Person attribute to set when a person leaves engagement status.", false, false, order: 6 )]
    [DisallowConcurrentExecution]
    public class CalculateEngagement : IJob
    {
        const int BATCH_SIZE = 100;

        public virtual void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            //
            // Get all our configuration options.
            //
            Guid engagedDataViewGuid = dataMap.GetString( "EngagedDataView" ).AsGuid();
            Guid? disengagedDataViewGuid = dataMap.GetString( "DisengagedDataView" ).AsGuidOrNull();
            Guid? entryWorkflowType = dataMap.GetString( "EntryWorkflow" ).AsGuidOrNull();
            Guid? exitWorkflowType = dataMap.GetString( "ExitWorkflow" ).AsGuidOrNull();
            Guid? engagedAttributeGuid = dataMap.GetString( "EngagedAttribute" ).AsGuidOrNull();
            Guid? startDateAttributeGuid = dataMap.GetString( "StartDateAttribute" ).AsGuidOrNull();
            Guid? endDateAttributeGuid = dataMap.GetString( "EndDateAttribute" ).AsGuidOrNull();

            //
            // Find the specified attributes in the cache.
            //
            var engagedAttribute = engagedAttributeGuid.HasValue ? AttributeCache.Get( engagedAttributeGuid.Value ) : null;
            var startDateAttribute = startDateAttributeGuid.HasValue ? AttributeCache.Get( startDateAttributeGuid.Value ) : null;
            var endDateAttribute = endDateAttributeGuid.HasValue ? AttributeCache.Get( endDateAttributeGuid.Value ) : null;

            using ( var rockContext = new RockContext() )
            {
                var workflowTypeService = new WorkflowTypeService( rockContext );

                var errorMessages = new List<string>();
                List<int> engagedDataViewPersonIds = new List<int>();
                List<int> disengagedDataViewPersonIds = null;
                List<int> engagingPersonIds = null;
                List<int> disengagingPersonIds = null;

                //
                // Get the list of people that are in the engaged data view.
                //
                try
                {
                    var dataViewService = new DataViewService( rockContext );
                    DataView dataView;
                    int dataTimeout = 900;

                    dataView = dataViewService.Get( engagedDataViewGuid );
                    var qry = dataView.GetQuery( null, rockContext, dataTimeout, out errorMessages );

                    if ( qry != null )
                    {
                        engagedDataViewPersonIds = qry.AsNoTracking().Select( a => a.Id ).ToList();
                    }

                    if ( disengagedDataViewGuid.HasValue )
                    {
                        dataView = dataViewService.Get( disengagedDataViewGuid.Value );
                        qry = dataView.GetQuery( null, rockContext, dataTimeout, out errorMessages );

                        if ( qry != null )
                        {
                            disengagedDataViewPersonIds = qry.AsNoTracking().Select( a => a.Id ).ToList();
                        }
                    }
                }
                catch ( Exception e )
                {
                    ExceptionLogService.LogException( e, System.Web.HttpContext.Current );

                    while ( e != null )
                    {
                        if ( e is SqlException && ( e as SqlException ).Number == -2 )
                        {
                            errorMessages.Add( "This dataview did not complete in a timely manner." );
                        }
                        else
                        {
                            errorMessages.Add( e.Message );
                        }

                        e = e.InnerException;
                    }
                }

                //
                // If we had any errors trying to run the data view then abort.
                //
                if ( errorMessages.Any() )
                {
                    throw new Exception( string.Join( "\n", errorMessages ) );
                }

                //
                // Get a list of all person Ids that exist in the database.
                //
                var personQry = new PersonService( rockContext ).Queryable().AsNoTracking();

                //
                // Calculate the list of people that are entering and leaving engagement based on
                // specific criteria:
                //
                // Have EngagedAttribute && Have DisengagedDataView
                //     EngagingPeople = EngagedDataView - AlreadyEngaged
                //     DisengagingPeople = DisengagedDataView - AlreadyDisengaged
                //
                // Have EngagedAttribute && No DisengagedDataView
                //     EngagingPeople = EngagedDataView - AlreadyEngaged
                //     DisengagingPeople = AlreadyEngaged - EngagedDataView
                //
                // No EngagedAttribute && Have DisengagedDataView
                //     EngagingPeople = EngagedDataView
                //     DisengagingPeople = DisengatedDataView
                //
                // No EngagedAttribute && No DisengagedDataView
                //     EngagingPeople = EngagedDataView
                //     DisengagingPeople = AllPeople - EngagedDataView
                //
                if ( engagedAttribute != null )
                {
                    var alreadyEngagedPersonIds = personQry.WhereAttributeValue( rockContext, engagedAttribute.Key, "True" ).Select( p => p.Id ).ToList();

                    engagingPersonIds = engagedDataViewPersonIds.Except( alreadyEngagedPersonIds ).ToList();

                    if ( disengagedDataViewPersonIds != null )
                    {
                        var alreadyDisengagedPersonIds = personQry.WhereAttributeValue( rockContext, engagedAttribute.Key, "False" ).Select( p => p.Id ).ToList();
                        disengagingPersonIds = disengagedDataViewPersonIds.Except( alreadyDisengagedPersonIds ).ToList();
                    }
                    else
                    {
                        disengagingPersonIds = alreadyEngagedPersonIds.Except( engagedDataViewPersonIds ).ToList();
                    }
                }
                else
                {
                    engagingPersonIds = engagedDataViewPersonIds;

                    if ( disengagedDataViewPersonIds != null )
                    {
                        disengagingPersonIds = disengagedDataViewPersonIds.ToList();
                    }
                    else
                    {
                        var allPersonIds = personQry.Select( p => p.Id ).ToList();
                        disengagingPersonIds = allPersonIds.Except( engagedDataViewPersonIds ).ToList();
                    }
                }

                //
                // Add any new people to engagement status.
                //
                if ( engagingPersonIds.Any() )
                {
                    ProcessPeople( engagingPersonIds, true, engagedAttribute, startDateAttribute, endDateAttribute, entryWorkflowType );
                }

                //
                // Remove any old people from engagement status.
                //
                if ( disengagingPersonIds.Any() )
                {
                    ProcessPeople( disengagingPersonIds, false, engagedAttribute, startDateAttribute, endDateAttribute, exitWorkflowType );
                }

                context.Result = string.Format( "Added {0} and removed {1} people from engaged status.", engagingPersonIds.Count, disengagingPersonIds.Count );
            }
        }

        /// <summary>
        /// Process the people for their engagement status.
        /// </summary>
        /// <param name="personIds">The list of person identifiers that need to be processed.</param>
        /// <param name="engaged">True if the person is becoming engaged, false if leaving engagement.</param>
        /// <param name="engagedAttribute">The attribute to store the true/false value of their engagement status.</param>
        /// <param name="startDateAttribute">The attribute to store the date they became engaged.</param>
        /// <param name="endDateAttribute">The attribute to store the date they left engagement.</param>
        /// <param name="workflowType">The workflow type to be launched for each person.</param>
        protected void ProcessPeople( List<int> personIds, bool engaged, AttributeCache engagedAttribute, AttributeCache startDateAttribute, AttributeCache endDateAttribute, Guid? workflowType )
        {
            while ( personIds.Any() )
            {
                using ( var rockContext = new RockContext() )
                {
                    //
                    // Work in batches so we don't overload the change tracker.
                    //
                    var batchPersonIds = personIds.Take( BATCH_SIZE ).ToList();
                    personIds = personIds.Skip( BATCH_SIZE ).ToList();
                    var people = new PersonService( rockContext ).Queryable()
                        .Where( p => batchPersonIds.Contains( p.Id ) )
                        .ToList();

                    foreach ( var person in people )
                    {
                        //
                        // Update person attributes.
                        //
                        if ( engagedAttribute != null || startDateAttribute != null || endDateAttribute != null )
                        {
                            person.LoadAttributes( rockContext );

                            //
                            // Always update the Engaged attribute.
                            //
                            if ( engagedAttribute != null )
                            {
                                SetPersonAttribute( rockContext, person, engagedAttribute, engaged.ToString() );
                            }

                            //
                            // Update the Start Date attribute only if we are becoming engaged. When leaving
                            // engagement we leave this set so they can see the period of time they were
                            // engaged.
                            //
                            if ( startDateAttribute != null && engaged )
                            {
                                SetPersonAttribute( rockContext, person, startDateAttribute, RockDateTime.Now.ToShortDateString() );
                            }

                            //
                            // Always update the End Date attribute. If we are leaving engagement then set the
                            // current date, otherwise make it blank (we are now engaged, so no end date yet).
                            //
                            if ( endDateAttribute != null )
                            {
                                SetPersonAttribute( rockContext, person, endDateAttribute, !engaged ? RockDateTime.Now.ToShortDateString() : string.Empty );
                            }
                        }
                    }

                    rockContext.SaveChanges();

                    //
                    // Launch the workflows.
                    //
                    if ( workflowType.HasValue )
                    {
                        foreach ( var person in people )
                        {
                            person.LaunchWorkflow( workflowType, person.FullName );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set the attribute value of a person and calculate the history change string.
        /// </summary>
        /// <param name="rockContext">The context to use when saving the data value.</param>
        /// <param name="person">The person to be updated.</param>
        /// <param name="attribute">The attribute whose value is to be updated.</param>
        /// <param name="value">The new value to be saved.</param>
        protected void SetPersonAttribute( RockContext rockContext, Person person, AttributeCache attribute, string value )
        {
            string originalValue = person.GetAttributeValue( attribute.Key );

            if ( ( originalValue ?? string.Empty ).Trim() != ( value ?? string.Empty ).Trim() )
            {
                Helper.SaveAttributeValue( person, attribute, value, rockContext );

                string formattedOriginalValue = string.Empty;
                if ( !string.IsNullOrWhiteSpace( originalValue ) )
                {
                    formattedOriginalValue = attribute.FieldType.Field.FormatValue( null, originalValue, attribute.QualifierValues, false );
                }

                string formattedNewValue = string.Empty;
                if ( !string.IsNullOrWhiteSpace( value ) )
                {
                    formattedNewValue = attribute.FieldType.Field.FormatValue( null, value, attribute.QualifierValues, false );
                }
            }
        }
    }
}
