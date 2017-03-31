using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_shepherdchurch.Misc
{
    [DisplayName( "Share Workflow" )]
    [Category( "com_shepherdchurch > Misc" )]
    [Description( "Export and import workflows from Rock." )]

    public partial class ShareWorkflow : RockBlock
    {
        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Initialize basic information about the page structure and setup the default content.
        /// </summary>
        /// <param name="sender">Object that is generating this event.</param>
        /// <param name="e">Arguments that describe this event.</param>
        protected void Page_Load( object sender, EventArgs e )
        {
            ScriptManager.GetCurrent( this.Page ).RegisterPostBackControl( btnExport );

            if ( !IsPostBack )
            {
            }
        }

        #endregion

        #region Core Methods


        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
        }

        #endregion

        protected void btnExport_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            var workflowTypeService = new WorkflowTypeService( rockContext );
            var workflowType = workflowTypeService.Get( wtpExport.SelectedValueAsId().Value );

            var container = Helper.ExportWorkflowType( workflowType );

            Page.EnableViewState = false;
            Page.Response.Clear();
            Page.Response.ContentType = "application/json";
            Page.Response.AppendHeader( "Content-Disposition", "attachment; filename=export.json" );
            Page.Response.Write( Newtonsoft.Json.JsonConvert.SerializeObject( container ) );
            Page.Response.Flush();
            Page.Response.End();
        }

        protected void fuImport_FileUploaded( object sender, EventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var binaryFileService = new BinaryFileService( rockContext );
                var binaryFile = binaryFileService.Get( fuImport.BinaryFileId ?? 0 );

                if ( binaryFile != null )
                {
                    try
                    {
                        var container = Newtonsoft.Json.JsonConvert.DeserializeObject<DataContainer>( binaryFile.ContentsToString() );
                        List<string> messages;

                        Helper.Import( container, true, rockContext, out messages );
                        ltDebug.Text = string.Empty;
                        foreach ( var msg in messages )
                        {
                            ltDebug.Text += string.Format( "{0}\n", msg );
                        }
                    }
                    finally
                    {
                        binaryFileService.Delete( binaryFile );
                        rockContext.SaveChanges();
                    }
                }

                fuImport.BinaryFileId = null;
            }
        }
    }

    /// <summary>
    /// Describes a single element of an entity path.
    /// </summary>
    class EntityPathComponent
    {
        /// <summary>
        /// The entity at this specific location in the path.
        /// </summary>
        public IEntity Entity { get; private set; }

        /// <summary>
        /// The name of the property used to reach the next location in the path.
        /// </summary>
        public string PropertyName { get; private set; }

        /// <summary>
        /// Create a new entity path component.
        /// </summary>
        /// <param name="entity">The entity at this specific location in the path.</param>
        /// <param name="propertyName">The name of the property used to reach the next location in the path.</param>
        public EntityPathComponent( IEntity entity, string propertyName )
        {
            Entity = entity;
            PropertyName = propertyName;
        }
    }

    /// <summary>
    /// Describes the entity and property path used to reach this point in
    /// the entity tree.
    /// </summary>
    class EntityPath : List<EntityPathComponent>
    {
        #region Instance Methods

        /// <summary>
        /// Create a duplicate copy of this entity path and return it.
        /// </summary>
        /// <returns>A duplicate of this entity path.</returns>
        public EntityPath Clone()
        {
            EntityPath path = new EntityPath();

            path.AddRange( this );

            return path;
        }

        /// <summary>
        /// Create a new EntityPath by appending the path component. The original
        /// path is not modified.
        /// </summary>
        /// <param name="component">The new path component to append to this path.</param>
        /// <returns></returns>
        public EntityPath PathByAddingComponent( EntityPathComponent component )
        {
            EntityPath path = this.Clone();

            path.Add( component );

            return path;
        }

        #endregion
    }

    /// <summary>
    /// Tracks entities and related information of entities that are queued up to be encoded.
    /// </summary>
    class QueuedEntity
    {
        #region Properties

        /// <summary>
        /// The entity that is queued up for processing.
        /// </summary>
        public IEntity Entity { get; private set; }

        /// <summary>
        /// A list of all paths that we took to reach this entity.
        /// </summary>
        public List<EntityPath> ReferencePaths { get; private set; }

        /// <summary>
        /// During the encode process this will be filled in with the encoded
        /// entity data so that we can keep a link between the IEntity and the
        /// encoded data until we are done.
        /// </summary>
        public EncodedEntity EncodedEntity { get; set; }

        #endregion

        #region Instance Methods

        /// <summary>
        /// Initialize a new queued entity for the given access path.
        /// </summary>
        /// <param name="entity">The entity that is going to be placed in the queue.</param>
        /// <param name="path">The initial path used to reach this entity.</param>
        public QueuedEntity( IEntity entity, EntityPath path )
        {
            Entity = entity;
            ReferencePaths = new List<EntityPath>();
            ReferencePaths.Add( path );
        }

        /// <summary>
        /// Add a new entity path reference to this existing entity.
        /// </summary>
        /// <param name="path">The path that can be used to reach this entity.</param>
        public void AddReferencePath( EntityPath path )
        {
            ReferencePaths.Add( path );
        }

        /// <summary>
        /// This is used for debug output to display the entity information and the path(s)
        /// that we took to find it.
        /// </summary>
        /// <returns>A string that describes this queued entity.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            EntityPath primaryPath = ReferencePaths[0];

            sb.AppendFormat( "{0} {1}", Entity.TypeName, Entity.Guid );
            foreach ( var p in ReferencePaths )
            {
                sb.AppendFormat( "\n\tPath" );
                foreach ( var e in p )
                {
                    sb.AppendFormat( "\n\t\t{0} {2} {1}", e.Entity.TypeName, e.PropertyName, e.Entity.Guid );
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Checks if this entity can be reached by the property path. For example if exporting a
        /// WorkflowType and you want to check if this entity is a WorkflowActionType for this
        /// workflow then you would use the property path "ActivityTypes.ActionTypes".
        /// </summary>
        /// <param name="propertyPath">The period delimited list of properties to reach this entity.</param>
        /// <returns>true if this entity can be reached by the property path, false if not.</returns>
        public bool ContainsPropertyPath( string propertyPath )
        {
            var properties = propertyPath.Split( '.' );

            foreach ( var path in ReferencePaths )
            {
                if ( path.Count == properties.Length )
                {
                    int i = 0;
                    for ( i = 0; i < path.Count; i++ )
                    {
                        if ( path[i].PropertyName != properties[i] )
                        {
                            break;
                        }
                    }

                    if ( i == path.Count )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion
    }

    /// <summary>
    /// A helper class for importing / exporting entities into and out of Rock.
    /// </summary>
    class Helper
    {
        #region Properties

        /// <summary>
        /// The list of entities that are queued up to be encoded. This is
        /// an ordered list and the entities will be encoded/decoded in this
        /// order.
        /// </summary>
        public List<QueuedEntity> Entities { get; private set; }

        /// <summary>
        /// The database context to perform all our operations in.
        /// </summary>
        public RockContext RockContext { get; private set; }

        /// <summary>
        /// The map of original Guids to newly generated Guids.
        /// </summary>
        private Dictionary<Guid, Guid> GuidMap { get; set; }

        #endregion

        #region Static Methods

        /// <summary>
        /// Export a WorkflowType and all required information.
        /// </summary>
        /// <param name="workflowType">The WorkflowType to be exported.</param>
        /// <returns>A DataContainer that is ready to be encoded and saved.</returns>
        static public DataContainer ExportWorkflowType( WorkflowType workflowType )
        {
            Helper helper = new Helper( new RockContext() );

            helper.EnqueueEntity( workflowType, new EntityPath() );

            return helper.ProcessQueue( ( queuedEntity ) =>
            {
                if ( queuedEntity.ContainsPropertyPath( "AttributeTypes" ) ||
                    queuedEntity.ContainsPropertyPath( "AttributeTypes.AttributeQualifiers" ) ||
                    queuedEntity.ContainsPropertyPath( "ActivityTypes" ) ||
                    queuedEntity.ContainsPropertyPath( "ActivityTypes.AttributeTypes" ) ||
                    queuedEntity.ContainsPropertyPath( "ActivityTypes.AttributeTypes.AttributeQualifiers" ) ||
                    queuedEntity.ContainsPropertyPath( "ActivityTypes.ActionTypes" ) ||
                    queuedEntity.ContainsPropertyPath( "ActivityTypes.ActionTypes.AttributeValues" ) ||
                    queuedEntity.ContainsPropertyPath( "ActivityTypes.ActionTypes.WorkflowFormId" ) ||
                    queuedEntity.ContainsPropertyPath( "ActivityTypes.ActionTypes.WorkflowFormId.FormAttributes" ) )
                {
                    return true;
                }
                else
                {
                    return false;
                }
            } );
        }

        /// <summary>
        /// Attempt to import the container of entities into the Rock database. Creates
        /// a transaction inside the RockContext to perform all the entity creation so
        /// if an error occurs everything will be left in a clean state.
        /// </summary>
        /// <param name="container">The container of all the encoded entities.</param>
        /// <param name="newGuids">Wether or not to generate new Guids during import of entities that have requested new Guids.</param>
        /// <param name="rockContext">The database context to operate in when creating and loading entities.</param>
        /// <param name="messages">Any messages, errors or otherwise, that should be displayed to the user.</param>
        /// <returns>true if the import succeeded, false if it did not.</returns>
        static public bool Import( DataContainer container, bool newGuids, RockContext rockContext, out List<string> messages )
        {
            messages = new List<string>();
            var helper = new Helper( rockContext );

            using ( var transaction = rockContext.Database.BeginTransaction() )
            {
                try
                {
                    //
                    // Generate a new Guid if we were asked to.
                    //
                    if ( newGuids )
                    {
                        foreach ( var encodedEntity in container.Entities )
                        {
                            if ( encodedEntity.NewGuid )
                            {
                                helper.MapNewGuid( encodedEntity.Guid );
                            }
                        }
                    }

                    //
                    // Walk each encoded entity and either verify an existing entity or
                    // create a new entity.
                    //
                    foreach ( var encodedEntity in container.Entities )
                    {
                        Type entityType = Reflection.FindType( typeof( IEntity ), encodedEntity.EntityType );
                        Guid entityGuid = helper.MapGuid( encodedEntity.Guid );
                        var entity = helper.GetExistingEntity( encodedEntity.EntityType, entityGuid );

                        if ( entity == null )
                        {
                            entity = helper.CreateNewEntity( encodedEntity );
                            messages.Add( string.Format( "Created: {0}, {1}", encodedEntity.EntityType, entityGuid ) );
                        }
                    }

                    transaction.Commit();

                    return true;
                }
                catch ( Exception e )
                {
                    transaction.Rollback();
                    messages.Add( e.Message );

                    return false;
                }
            }
        }

        #endregion

        #region Instance Methods

        /// <summary>
        /// Initialize a new Helper object for facilitating the export/import of entities.
        /// </summary>
        /// <param name="rockContext">The RockContext to work in when exporting or importing.</param>
        protected Helper( RockContext rockContext )
        {
            Entities = new List<QueuedEntity>();
            GuidMap = new Dictionary<Guid, Guid>();
            RockContext = rockContext;
        }

        /// <summary>
        /// Process the queued list of entities that are waiting to be encoded. This
        /// encodes all entities, generates new Guids for any entities that need them,
        /// and then maps all references to the new Guids.
        /// </summary>
        /// <param name="guidEvaluation">A function that is called for each entity to determine if it needs a new Guid or not.</param>
        /// <returns>A DataContainer that is ready for JSON export.</returns>
        protected DataContainer ProcessQueue( Func<QueuedEntity, bool> guidEvaluation )
        {
            DataContainer container = new DataContainer();

            //
            // Find out if we need to give new Guid values to any entities.
            //
            foreach ( var queuedEntity in Entities )
            {
                queuedEntity.EncodedEntity = Export( queuedEntity.Entity );

                if ( queuedEntity.ReferencePaths[0].Count == 0 || guidEvaluation( queuedEntity ) )
                {
                    queuedEntity.EncodedEntity.NewGuid = true;
                }
            }

            //
            // Convert to a data container.
            //
            foreach ( var queuedEntity in Entities )
            {
                container.Entities.Add( queuedEntity.EncodedEntity );

                if ( queuedEntity.ReferencePaths.Count == 1 && queuedEntity.ReferencePaths[0].Count == 0 )
                {
                    container.RootEntities.Add( queuedEntity.EncodedEntity.Guid );
                }
            }

            return container;
        }

        /// <summary>
        /// Adds an entity to the queue list. This provides circular reference checking as
        /// well as ensuring that proper order is maintained for all entities.
        /// </summary>
        /// <param name="entity">The entity that is to be included in the export.</param>
        /// <param name="path">The entity path that lead to this entity being encoded.</param>
        protected void EnqueueEntity( IEntity entity, EntityPath path )
        {
            List<KeyValuePair<string, IEntity>> entities;

            //
            // These are system generated rows, we should never try to backup or restore them.
            //
            if ( entity.TypeName == "Rock.Model.EntityType" || entity.TypeName == "Rock.Model.FieldType" )
            {
                return;
            }

            //
            // If the entity is already in our path that means we are beginning a circular
            // reference so we can just ignore this one.
            //
            if ( path.Where( e => e.Entity.Guid == entity.Guid ).Any() )
            {
                return;
            }

            //
            // Find the entities that this entity references, in other words entities that must
            // exist before this one can be created.
            //
            entities = FindReferencedEntities( entity );
            entities.ForEach( e => EnqueueEntity( e.Value, path.PathByAddingComponent( new EntityPathComponent( entity, e.Key ) ) ) );

            //
            // If we already know about the entity, add a reference to it and return.
            //
            var queuedEntity = Entities.Where( e => e.Entity.Guid == entity.Guid ).FirstOrDefault();
            if ( queuedEntity == null )
            {
                Entities.Add( queuedEntity = new QueuedEntity( entity, path.Clone() ) );
            }
            else
            {
                queuedEntity.AddReferencePath( path.Clone() );
            }

            //
            // Find the entities that this entity has as children. This is usually the many side
            // of a one-to-many reference (such as a Workflow has many WorkflowActions, this would
            // get a list of the WorkflowActions).
            //
            entities = FindChildEntities( entity );
            entities.ForEach( e => EnqueueEntity( e.Value, path.PathByAddingComponent( new EntityPathComponent( entity, e.Key ) ) ) );
        }

        /// <summary>
        /// Find entities that this object references directly. These are entities that must be
        /// created before this entity can be re-created.
        /// </summary>
        /// <param name="parentEntity"></param>
        /// <returns></returns>
        public List<KeyValuePair<string, IEntity>> FindReferencedEntities( IEntity parentEntity )
        {
            List<KeyValuePair<string, IEntity>> children = new List<KeyValuePair<string, IEntity>>();

            var properties = GetEntityProperties( parentEntity );

            //
            // Take a stab at any properties that end in "Id" and likely reference another
            // entity, such as a property called "WorkflowId" probably references the Workflow
            // entity and should be linked by Guid.
            //
            foreach ( var property in properties )
            {
                if ( property.Name.EndsWith( "Id" ) && ( property.PropertyType == typeof( int ) || property.PropertyType == typeof( Nullable<int> ) ) )
                {
                    var entityProperty = parentEntity.GetType().GetProperty( property.Name.Substring( 0, property.Name.Length - 2 ) );

                    if ( entityProperty != null )
                    {
                        IEntity childEntity = entityProperty.GetValue( parentEntity ) as IEntity;

                        if ( childEntity != null )
                        {
                            children.Add( new KeyValuePair<string, IEntity>( property.Name, childEntity ) );
                        }
                    }
                }
            }

            //
            // Allow for processors to adjust the list of children.
            //
            Type processorBaseType = typeof( EntityProcessor<> ).MakeGenericType( GetEntityType( parentEntity ) );
            foreach ( var processorType in Reflection.FindTypes( processorBaseType ) )
            {
                IEntityProcessor processor = ( IEntityProcessor ) Activator.CreateInstance( processorType.Value );

                processor.EvaluateReferencedEntities( parentEntity, children, this );
            }

            return children;
        }

        /// <summary>
        /// Generate the list of entities that refernce this parent entity. These are entities that
        /// must be created after this entity has been created.
        /// </summary>
        /// <param name="parentEntity">The parent entity to find reverse-references to.</param>
        /// <returns></returns>
        public List<KeyValuePair<string, IEntity>> FindChildEntities( IEntity parentEntity )
        {
            List<KeyValuePair<string, IEntity>> children = new List<KeyValuePair<string, IEntity>>();

            var properties = GetEntityProperties( parentEntity );

            //
            // Take a stab at any properties that are an ICollection<IEntity> and treat those
            // as child entities.
            //
            foreach ( var property in properties )
            {
                if ( property.PropertyType.GetInterface( "IEnumerable" ) != null && property.PropertyType.GetGenericArguments().Length == 1 )
                {
                    if ( typeof( IEntity ).IsAssignableFrom( property.PropertyType.GetGenericArguments()[0] ) )
                    {
                        IEnumerable childEntities = ( IEnumerable ) property.GetValue( parentEntity );

                        foreach ( IEntity childEntity in childEntities )
                        {
                            children.Add( new KeyValuePair<string, IEntity>( property.Name, childEntity ) );
                        }
                    }
                }
            }

            //
            // We also need to pull in any attribute values. We have to pull attributes as well
            // since we might not have an actual value for that attribute yet and would need
            // it to pull the default value and definition.
            //
            var attributedEntity = parentEntity as IHasAttributes;
            if ( attributedEntity != null )
            {
                if ( attributedEntity.Attributes == null )
                {
                    attributedEntity.LoadAttributes( RockContext );
                }

                foreach ( var item in attributedEntity.Attributes )
                {
                    var attrib = new AttributeService( RockContext ).Get( item.Value.Guid );

                    children.Add( new KeyValuePair<string, IEntity>( "Attributes", attrib ) );

                    var value = new AttributeValueService( RockContext ).Queryable()
                        .Where( v => v.AttributeId == attrib.Id && v.EntityId == attributedEntity.Id )
                        .FirstOrDefault();
                    if ( value != null )
                    {
                        children.Add( new KeyValuePair<string, IEntity>( "AttributeValues", value ) );
                    }
                }
            }

            //
            // Allow for processors to adjust the list of children.
            //
            Type processorBaseType = typeof( EntityProcessor<> ).MakeGenericType( GetEntityType( parentEntity ) );
            foreach ( var processorType in Reflection.FindTypes( processorBaseType ) )
            {
                IEntityProcessor processor = ( IEntityProcessor ) Activator.CreateInstance( processorType.Value );

                processor.EvaluateChildEntities( parentEntity, children, this );
            }

            return children;
        }

        /// <summary>
        /// Get the list of properties from the entity that should be stored or re-created.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public List<PropertyInfo> GetEntityProperties( IEntity entity )
        {
            //
            // Get all data member mapped properties and filter out any "local only"
            // properties that should not be exported.
            //
            return GetEntityType( entity )
                .GetProperties()
                .Where( p => System.Attribute.IsDefined( p, typeof( DataMemberAttribute ) ) )
                .Where( p => !System.Attribute.IsDefined( p, typeof( NotMappedAttribute ) ) )
                .Where( p => !System.Attribute.IsDefined( p, typeof( DatabaseGeneratedAttribute ) ) )
                .Where( p => p.Name != "Id" && p.Name != "Guid" )
                .Where( p => p.Name != "ForeignId" && p.Name != "ForeignGuid" && p.Name != "ForeignKey" )
                .Where( p => p.Name != "CreatedByPersonAliasId" && p.Name != "ModifiedByPersonAliasId" )
                .ToList();
        }

        /// <summary>
        /// Determines the real Entity type of the given IEntity object. Because
        /// many IEntity objects are dynamic proxies created by Entity Framework
        /// we need to get the actual underlying type.
        /// </summary>
        /// <param name="entity">The entity whose type we want to obtain.</param>
        /// <returns>The true IEntity type (such as Rock.Model.Person).</returns>
        public Type GetEntityType( IEntity entity )
        {
            Type type = entity.GetType();

            return type.IsDynamicProxyType() ? type.BaseType : type;
        }

        /// <summary>
        /// Creates a new map entry for the oldGuid. This generates a new Guid and
        /// stores a reference between the two.
        /// </summary>
        /// <param name="oldGuid">The original Guid value to be mapped from.</param>
        /// <returns>A new Guid value that should be used in place of oldGuid.</returns>
        public Guid MapNewGuid( Guid oldGuid )
        {
            GuidMap.Add( oldGuid, Guid.NewGuid() );

            return GuidMap[oldGuid];
        }

        /// <summary>
        /// Finds and returns a Guid from the mapping dictionary. If no mapping
        /// exists then the original Guid is returned.
        /// </summary>
        /// <param name="oldGuid">The original Guid value to map from.</param>
        /// <returns>The Guid value that should be used, may be the same as oldGuid.</returns>
        public Guid MapGuid( Guid oldGuid )
        {
            return GuidMap.ContainsKey( oldGuid ) ? GuidMap[oldGuid] : oldGuid;
        }

        /// <summary>
        /// Export the given entity into an EncodedEntity object. This can be used later to
        /// reconstruct the entity.
        /// </summary>
        /// <param name="entity">The entity to be exported.</param>
        /// <returns>The exported data that can be imported.</returns>
        protected EncodedEntity Export( IEntity entity )
        {
            EncodedEntity encodedEntity = new EncodedEntity();
            Type entityType = GetEntityType( entity );

            encodedEntity.Guid = entity.Guid;
            encodedEntity.EntityType = entityType.FullName;

            var attributeEntity = entity as Rock.Model.Attribute;
            if ( attributeEntity != null )
            {
                if ( attributeEntity.EntityTypeQualifierColumn == "WorkflowTypeId" )
                {
                }
            }

            //
            // Generate the standard properties and references.
            //
            foreach ( var property in GetEntityProperties( entity ) )
            {
                //
                // Don't encode IEntity properties, we should have the Id encoded instead.
                //
                if ( typeof( IEntity ).IsAssignableFrom( property.PropertyType ) )
                {
                    continue;
                }

                //
                // Don't encode IEnumerable properties. Those should be included as
                // their own entities to be encoded later.
                //
                if ( property.PropertyType.GetInterface( "IEnumerable" ) != null &&
                    property.PropertyType.GetGenericArguments().Length == 1 &&
                    typeof( IEntity ).IsAssignableFrom( property.PropertyType.GetGenericArguments()[0] ) )
                {
                    continue;
                }

                encodedEntity.Properties.Add( property.Name, property.GetValue( entity ) );
            }

            GenerateReferences( entity, encodedEntity );

            return encodedEntity;
        }

        /// <summary>
        /// Generate any explicit references to other objects in a manner that we can use
        /// to recreate those references after import.
        /// </summary>
        /// <param name="entity">The entity whose references need to be defined.</param>
        /// <param name="exportData">The export data to defined the references in.</param>
        protected void GenerateReferences( IEntity entity, EncodedEntity exportData )
        {
            foreach ( var x in FindReferencedEntities( entity ) )
            {
                exportData.MakeGuidReference( GetEntityType( x.Value ), x.Key, x.Value.Guid );
            }
        }

        /// <summary>
        /// Attempt to load an entity from the database based on it's Guid and entity type.
        /// </summary>
        /// <param name="entityType">The type of entity to load.</param>
        /// <param name="guid">The unique identifier of the entity.</param>
        /// <returns>The loaded entity or null if not found.</returns>
        public IEntity GetExistingEntity( string entityType, Guid guid )
        {
            var service = Reflection.GetServiceForEntityType( Reflection.FindType( typeof( IEntity ), entityType ), RockContext );

            if ( service != null )
            {
                var getMethod = service.GetType().GetMethod( "Get", new Type[] { typeof( Guid ) } );

                if ( getMethod != null )
                {
                    return ( IEntity ) getMethod.Invoke( service, new object[] { guid } );
                }
            }

            return null;
        }

        /// <summary>
        /// Attempt to load an entity from the database based on it's Guid and entity type.
        /// </summary>
        /// <param name="entityType">The type of entity to load.</param>
        /// <param name="guid">The unique identifier of the entity.</param>
        /// <returns>The loaded entity or null if not found.</returns>
        public IEntity GetExistingEntity( string entityType, int id )
        {
            var service = Reflection.GetServiceForEntityType( Reflection.FindType( typeof( IEntity ), entityType ), RockContext );

            if ( service != null )
            {
                var getMethod = service.GetType().GetMethod( "Get", new Type[] { typeof( int ) } );

                if ( getMethod != null )
                {
                    return ( IEntity ) getMethod.Invoke( service, new object[] { id } );
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a new entity in the database from the encoded information. The entity
        /// is saved before being returned.
        /// </summary>
        /// <param name="encodedEntity">The encoded entity information to create the new entity from.</param>
        /// <returns>A reference to the new entity.</returns>
        protected IEntity CreateNewEntity( EncodedEntity encodedEntity )
        {
            Type entityType = Reflection.FindType( typeof( IEntity ), encodedEntity.EntityType );
            var service = Reflection.GetServiceForEntityType( entityType, RockContext );

            if ( service != null )
            {
                var addMethod = service.GetType().GetMethod( "Add", new Type[] { entityType } );

                if ( addMethod != null )
                {
                    IEntity entity = ( IEntity ) Activator.CreateInstance( entityType );

                    RestoreEntityProperties( entity, encodedEntity );
                    entity.Guid = MapGuid( encodedEntity.Guid );

                    //
                    // Do custom pre-save processing.
                    //
                    Type processorBaseType = typeof( EntityProcessor<> ).MakeGenericType( entityType );
                    foreach ( var processorType in Reflection.FindTypes( processorBaseType ) )
                    {
                        IEntityProcessor processor = ( IEntityProcessor ) Activator.CreateInstance( processorType.Value );

                        processor.PreProcessImportedEntity( entity, encodedEntity, encodedEntity.GetTransform( processorType.Value.FullName ), this );
                    }

                    addMethod.Invoke( service, new object[] { entity } );
                    RockContext.SaveChanges();

                    return entity;
                }
            }

            throw new Exception( string.Format( "Failed to create new database entity for {0}_{1}", encodedEntity.EntityType, encodedEntity.Guid ) );
        }

        /// <summary>
        /// Restore the property information from encodedEntity into the newly created entity.
        /// </summary>
        /// <param name="entity">The blank entity to be populated.</param>
        /// <param name="encodedEntity">The encoded entity data.</param>
        protected void RestoreEntityProperties( IEntity entity, EncodedEntity encodedEntity )
        {
            foreach ( var property in GetEntityProperties( entity ) )
            {
                //
                // If this is a plain property, just set the value.
                //
                if ( encodedEntity.Properties.ContainsKey( property.Name ) )
                {
                    var value = encodedEntity.Properties[property.Name];

                    //
                    // If this is a Guid, see if we need to remap it.
                    //
                    Guid? guidValue = null;
                    if ( value is Guid )
                    {
                        guidValue = ( Guid ) value;
                        value = MapGuid( guidValue.Value );
                    }
                    else if ( value is string )
                    {
                        guidValue = ( ( string ) value ).AsGuidOrNull();
                        if ( guidValue.HasValue && guidValue.Value != MapGuid( guidValue.Value ) )
                        {
                            value = MapGuid( guidValue.Value ).ToString();
                        }
                    }

                    property.SetValue( entity, ChangeType( property.PropertyType, value ) );
                }
                else
                {
                    //
                    // Otherwise check if it is a reference property.
                    //
                    var reference = encodedEntity.References.Where( r => r.Property == property.Name ).FirstOrDefault();

                    if ( reference != null )
                    {
                        var otherEntity = GetExistingEntity( reference.EntityType, MapGuid( reference.Guid ) );

                        if ( otherEntity != null )
                        {
                            var idProperty = otherEntity.GetType().GetProperty( "Id" );

                            if ( idProperty != null )
                            {
                                property.SetValue( entity, ChangeType( property.PropertyType, idProperty.GetValue( otherEntity ) ) );
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Convert the given object value to the target type. This extends
        /// the IConvertable.ChangeType support since some things don't
        /// implement IConvertable, like Guid and Nullable.
        /// </summary>
        /// <param name="t">The target data type to convert to.</param>
        /// <param name="obj">The object value to be converted.</param>
        /// <returns>The value converted to the target type.</returns>
        public object ChangeType( Type t, object obj )
        {
            Type u = Nullable.GetUnderlyingType( t );

            if ( u != null )
            {
                return ( obj == null ) ? null : ChangeType( u, obj );
            }
            else
            {
                if ( t.IsEnum )
                {
                    return Enum.Parse( t, obj.ToString() );
                }
                else if ( t == typeof( Guid ) && obj is string )
                {
                    return new Guid( ( string ) obj );
                }
                else if ( t == typeof( string ) && obj is Guid )
                {
                    return obj.ToString();
                }
                else
                {
                    return Convert.ChangeType( obj, t );
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Most entities in Rock reference other entities by Id number, i.e. CategoryId.
    /// This is not useful when exporting/importing entities between systems. So we
    /// embed a Reference object that contains the Property name that originally
    /// contained the Id number. During an import operation that Property is filled in
    /// with the Id number of the object identified by the EntityType and the Guid.
    /// </summary>
    class Reference
    {
        #region Properties

        /// <summary>
        /// The name of the property to be filled in with the Id number of the
        /// referenced entity.
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// The entity type name that will be loaded by it's Guid.
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// The unique identifier of the EntityType to load.
        /// </summary>
        public Guid Guid { get; set; }

        #endregion

        #region Instance Methods

        /// <summary>
        /// Creates a new entity reference object that is used to reconstruct the
        /// link between two entities in the database.
        /// </summary>
        /// <param name="property">The name of the property in the containing entity.</param>
        /// <param name="entityType">The referenced entity type name.</param>
        /// <param name="guid">The identifier of the referenced entity.</param>
        public Reference( string property, string entityType, Guid guid )
        {
            Property = property;
            EntityType = entityType;
            Guid = guid;
        }

        #endregion
    }

    /// <summary>
    /// General container for exported entity data. This object should be encoded
    /// and decoded as JSON data.
    /// </summary>
    class DataContainer
    {
        #region Properties

        /// <summary>
        /// The encoded entities that will be used to identify all the database
        /// entities that are to be recreated.
        /// </summary>
        public List<EncodedEntity> Entities { get; private set; }

        /// <summary>
        /// The Guid values of the root entities that were used when exporting.
        /// </summary>
        public List<Guid> RootEntities { get; private set; }

        #endregion

        #region Instance Methods

        /// <summary>
        /// Create a new, empty, instance of the data container.
        /// </summary>
        public DataContainer()
        {
            Entities = new List<EncodedEntity>();
            RootEntities = new List<Guid>();
        }

        #endregion
    }

    /// <summary>
    /// Describes an Entity object in a portable manner that can be used
    /// to re-create the entity on another Rock installation.
    /// </summary>
    class EncodedEntity
    {
        #region Properties

        /// <summary>
        /// The entity class name that we are describing.
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// The guid to use to check if this entity already exists.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// Specifies if this entity should be allowed to get a new Guid during import.
        /// </summary>
        public bool NewGuid { get; set; }

        /// <summary>
        /// The values that describe the entities properties.
        /// </summary>
        public Dictionary<string, object> Properties { get; private set; }

        /// <summary>
        /// Any processor transform data that is needed to re-create the entity.
        /// </summary>
        public Dictionary<string, object> Transforms { get; private set; }

        /// <summary>
        /// List of references that will be used to re-create inter-entity references.
        /// </summary>
        public List<Reference> References { get; private set; }

        #endregion

        #region Instance Methods

        /// <summary>
        /// Create a new instance of an encoded entity.
        /// </summary>
        public EncodedEntity()
        {
            Properties = new Dictionary<string, object>();
            Transforms = new Dictionary<string, object>();
            References = new List<Reference>();
        }

        /// <summary>
        /// Replace a "by id" property that references another entity with a reference
        /// object that contains the information we will need to re-create that property
        /// at import time.
        /// </summary>
        /// <param name="entityType">The data type of the entity being referenced.</param>
        /// <param name="originalProperty">The original property name that we are replacing.</param>
        /// <param name="guid">The Guid that can be used later to lookup the referenced entity.</param>
        public void MakeGuidReference( Type entityType, string originalProperty, Guid guid )
        {
            Reference reference = new Reference( originalProperty, entityType.FullName, guid );

            References.Add( reference );
            Properties.Remove( originalProperty );
        }

        /// <summary>
        /// Get the transform value for the given processor.
        /// </summary>
        /// <param name="name">The full class name of the processor.</param>
        /// <returns>An object containing the data for the processor, or null if none was found.</returns>
        public object GetTransform( string name )
        {
            if ( Transforms.ContainsKey( name ) )
            {
                return ( IDictionary<string, object> ) Transforms[name];
            }

            return null;
        }

        /// <summary>
        /// Add a new transform object to this encoded entity. These are used by EntityProcessor
        /// implementations to facilitate in exporting and imported complex entities that need
        /// a little extra customization done to them.
        /// </summary>
        /// <param name="name">The name of the transform, this is the full class name of the processor.</param>
        /// <param name="value">The black box value for the transform.</param>
        public void AddTransform( string name, object value )
        {
            Transforms.Add( name, value );
        }

        #endregion
    }

    /// <summary>
    /// Interface for indicating that the inheriting class is a processor for
    /// export and import of entities from and to Rock.
    /// </summary>
    interface IEntityProcessor
    {
        /// <summary>
        /// Evaluate the list of referenced entities. This is a list of key value pairs that identify
        /// the property that the reference came from as well as the referenced entity itself. Implementations
        /// of this method may add or remove from this list. For example, an AttributeValue has
        /// the entity it is referencing in a EntityId column, but there is no general use information for
        /// what kind of entity it is. The processor can provide that information.
        /// </summary>
        /// <param name="entity">The parent entity of the references.</param>
        /// <param name="children">The referenced entities and what properties of the parent they came from.</param>
        /// <param name="helper">The helper class for this export.</param>
        void EvaluateReferencedEntities( IEntity entity, List<KeyValuePair<string, IEntity>> children, Helper helper );

        /// <summary>
        /// Evaluate the list of child entities. This is a list of key value pairs that identify
        /// the property that the child came from as well as the child entity itself. Implementations
        /// of this method may add or remove from this list. For example, a WorkflowActionForm has
        /// it's actions encoded in a single string. This should must processed to include any other
        /// objects that should exist (such as a DefinedValue for the button type).
        /// </summary>
        /// <param name="entity">The parent entity of the children.</param>
        /// <param name="children">The child entities and what properties of the parent they came from.</param>
        /// <param name="helper">The helper class for this export.</param>
        void EvaluateChildEntities( IEntity entity, List<KeyValuePair<string, IEntity>> children, Helper helper );

        /// <summary>
        /// An entity has been exported and can now have any post-processing done to it
        /// that is needed. For example a processor might remove some properties that shouldn't
        /// actually be exported.
        /// </summary>
        /// <param name="entity">The source entity that was exported.</param>
        /// <param name="encodedEntity">The exported data from the entity.</param>
        /// <param name="helper">The helper that is doing the exporting.</param>
        /// <returns>An object that will be encoded with the entity and passed to the ProcessImportEntity method later, or null.</returns>
        object PostProcessExportedEntity( IEntity entity, EncodedEntity encodedEntity, Helper helper );

        /// <summary>
        /// This method is called before the entity is saved and allows any final changes to the
        /// entity before it is stored in the database. Any Guid references that are not standard
        /// properties must also be updated, such as the Actions string of a WorkflowActionForm.
        /// </summary>
        /// <param name="entity">The in-memory entity that is about to be saved.</param>
        /// <param name="encodedEntity">The encoded information that was used to reconstruct the entity.</param>
        /// <param name="data">Custom data that was previously returned by ProcessExportedEntity.</param>
        /// <param name="helper">The helper in charge of the import process.</param>
        void PreProcessImportedEntity( IEntity entity, EncodedEntity encodedEntity, object data, Helper helper );

        /// <summary>
        /// This method is called after all entities have been imported. If there are any last
        /// minute changes to the entity that are needed to be made after other objects have been
        /// created then they may be made here. If any changes are made to the object then true
        /// must be returned to indicate a need to re-save the entity, otherwise return false.
        /// </summary>
        /// <param name="entity">The entity that now exists in the database.</param>
        /// <param name="encodedEntity">The encoded information that was used to reconstruct the entity.</param>
        /// <param name="data">Custom data that was previously returned by ProcessExportedEntity.</param>
        /// <param name="helper">The helper in charge of the import process.</param>
        /// <returns>true if the entity needs to be saved again, otherwise false.</returns>
        bool PostProcessImportedEntity( IEntity entity, EncodedEntity encodedEntity, object data, Helper helper );
    }

    /// <summary>
    /// Entity processors must inherit from this class to be able to provide
    /// custom processing capabilities.
    /// </summary>
    /// <typeparam name="T">The IEntity class type that this processor is for.</typeparam>
    abstract class EntityProcessor<T> : IEntityProcessor where T : IEntity
    {
        public void EvaluateReferencedEntities( IEntity entity, List<KeyValuePair<string, IEntity>> references, Helper helper )
        {
            EvaluateReferencedEntities( ( T ) entity, references, helper );
        }

        protected virtual void EvaluateReferencedEntities( T entity, List<KeyValuePair<string, IEntity>> references, Helper helper )
        {
        }


        public void EvaluateChildEntities( IEntity entity, List<KeyValuePair<string, IEntity>> children, Helper helper )
        {
            EvaluateChildEntities( ( T ) entity, children, helper );
        }

        protected virtual void EvaluateChildEntities( T entity, List<KeyValuePair<string, IEntity>> children, Helper helper )
        {
        }


        public object PostProcessExportedEntity( IEntity entity, EncodedEntity encodedEntity, Helper helper )
        {
            return PostProcessExportedEntity( ( T ) entity, encodedEntity, helper );
        }

        protected virtual object PostProcessExportedEntity( T entity, EncodedEntity encodedEntity, Helper helper )
        {
            return null;
        }


        public void PreProcessImportedEntity( IEntity entity, EncodedEntity encodedEntity, object data, Helper helper )
        {
            PreProcessImportedEntity( ( T ) entity, encodedEntity, data, helper );
        }

        public virtual void PreProcessImportedEntity( T entity, EncodedEntity encodedEntity, object data, Helper helper )
        {
        }


        public bool PostProcessImportedEntity( IEntity entity, EncodedEntity encodedEntity, object data, Helper helper )
        {
            return PostProcessImportedEntity( ( T ) entity, encodedEntity, data, helper );
        }

        public virtual bool PostProcessImportedEntity( T entity, EncodedEntity encodedEntity, object data, Helper helper )
        {
            return false;
        }
    }

    class WorkflowTypeProcessor : EntityProcessor<WorkflowType>
    {
        protected override void EvaluateChildEntities( WorkflowType entity, List<KeyValuePair<string, IEntity>> children, Helper helper )
        {
            var attributeService = new AttributeService( helper.RockContext );

            var items = attributeService
                .GetByEntityTypeId( new Workflow().TypeId ).AsQueryable()
                .Where( a =>
                    a.EntityTypeQualifierColumn.Equals( "WorkflowTypeId", StringComparison.OrdinalIgnoreCase ) &&
                    a.EntityTypeQualifierValue.Equals( entity.Id.ToString() ) )
                .ToList();

            //
            // We have to special process the attributes since we modify them.
            //
            foreach ( var item in items )
            {
                children.Add( new KeyValuePair<string, IEntity>( "AttributeTypes", item ) );
            }
        }
    }

    class WorkflowActivityTypeProcessor : EntityProcessor<WorkflowActivityType>
    {
        protected override void EvaluateChildEntities( WorkflowActivityType entity, List<KeyValuePair<string, IEntity>> children, Helper helper )
        {
            var attributeService = new AttributeService( helper.RockContext );

            var items = attributeService
                .GetByEntityTypeId( new WorkflowActivity().TypeId ).AsQueryable()
                .Where( a =>
                    a.EntityTypeQualifierColumn.Equals( "ActivityTypeId", StringComparison.OrdinalIgnoreCase ) &&
                    a.EntityTypeQualifierValue.Equals( entity.Id.ToString() ) )
                .ToList();

            //
            // We have to special process the attributes since we modify them.
            //
            foreach ( var item in items )
            {
                children.Add( new KeyValuePair<string, IEntity>( "AttributeTypes", item ) );
            }
        }
    }

    class AttributeProcessor : EntityProcessor<Rock.Model.Attribute>
    {
        protected override void EvaluateReferencedEntities( Rock.Model.Attribute entity, List<KeyValuePair<string, IEntity>> references, Helper helper )
        {
            int entityId;

            if ( entity.EntityTypeQualifierColumn.EndsWith( "Id" ) && int.TryParse( entity.EntityTypeQualifierValue, out entityId ) )
            {
                var entityType = Reflection.FindType( typeof( IEntity ), entity.EntityType.Name );

                if ( entityType != null )
                {
                    var property = entityType.GetProperty( entity.EntityTypeQualifierColumn.ReplaceLastOccurrence( "Id", string.Empty ) );

                    if ( property != null )
                    {
                        if ( typeof( IEntity ).IsAssignableFrom( property.PropertyType ) )
                        {
                            var target = helper.GetExistingEntity( property.PropertyType.FullName, entityId );
                            if ( target != null )
                            {
                                references.Add( new KeyValuePair<string, IEntity>( "EntityTypeQualifierValue", target ) );
                            }
                            else
                            {
                                throw new Exception( string.Format( "Could not find referenced qualifier of Attribute {0}", entity.Guid ) );
                            }
                        }
                    }
                }
            }
        }
    }

    class AttributeValueProcessor : EntityProcessor<AttributeValue>
    {
        protected override void EvaluateReferencedEntities( AttributeValue entity, List<KeyValuePair<string, IEntity>> references, Helper helper )
        {
            if ( entity.EntityId.HasValue && entity.Attribute != null )
            {
                var target = helper.GetExistingEntity( entity.Attribute.EntityType.Name, entity.EntityId.Value );
                if ( target != null )
                {
                    references.Add( new KeyValuePair<string, IEntity>( "EntityId", target ) );
                }
                else
                {
                    throw new Exception( string.Format( "Cannot export AttributeValue {0} because we cannot determine what entity it references.", entity.Guid ) );
                }
            }
        }
    }

    class WorkflowActionFormProcessor : EntityProcessor<WorkflowActionForm>
    {
        public override void PreProcessImportedEntity( WorkflowActionForm entity, EncodedEntity encodedEntity, object data, Helper helper )
        {
            //
            // Update the Guids in all the action buttons.
            //
            List<string> actions = entity.Actions.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
            for ( int i = 0; i < actions.Count; i++ )
            {
                var details = actions[i].Split( new char[] { '^' } );
                if ( details.Length > 2 )
                {
                    Guid definedValueGuid = details[1].AsGuid();
                    Guid activityTypeGuid = details[2].AsGuid();

                    details[1] = helper.MapGuid( definedValueGuid ).ToString();
                    details[2] = helper.MapGuid( activityTypeGuid ).ToString();

                    actions[i] = string.Join( "^", details );
                }
            }

            entity.Actions = string.Join( "|", actions );
        }
    }
}
