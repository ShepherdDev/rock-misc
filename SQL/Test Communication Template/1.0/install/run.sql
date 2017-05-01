DECLARE @BlockTypeGuid AS UNIQUEIDENTIFIER = '8988971D-6B8F-4759-ABD4-F44EAD3F879F'
DECLARE @BlockSystemEmailsGuid AS UNIQUEIDENTIFIER = '1594A6D4-8561-404D-9797-E0D1A1456782'
DECLARE @BlockCommunicationTemplatesGuid AS UNIQUEIDENTIFIER = '5A1E5168-7FB9-4EA5-8575-18FB4FFDEC20'
DECLARE @BlockAttributeCommunicationTypeGuid AS UNIQUEIDENTIFIER = 'B85DB007-5AC9-4458-9F68-05807408241C'

DECLARE @SystemEmailsPageGuid AS UNIQUEIDENTIFIER = '89B7A631-EA6F-4DA3-9380-04EE67B63E9E'
DECLARE @CommunicationTemplatesPageGuid AS UNIQUEIDENTIFIER = '39F75137-90D2-4E6F-8613-F19344767594'

DECLARE @SystemEmailsPageId AS INT = (SELECT [Id] FROM [Page] WHERE [Guid] = @SystemEmailsPageGuid)
DECLARE @CommunicationTemplatesPageId AS INT = (SELECT [Id] FROM [Page] WHERE [Guid] = @CommunicationTemplatesPageGuid)
DECLARE @BlockEntityTypeId AS INT = (SELECT [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.Block')
DECLARE @SingleSelectFieldTypeId AS INT = (SELECT [Id] FROM [FieldType] WHERE [Class] = 'Rock.Field.Types.SelectSingleFieldType')
DECLARE @BlockTypeId AS INT
DECLARE @CommunicationTypeAttributeId AS INT
DECLARE @BlockId AS INT

-- Create the block type.
INSERT INTO [BlockType]
	([IsSystem], [Path], [Name], [Description], [Guid], [Category])
	VALUES
	(0, '~/Plugins/com_shepherdchurch/Misc/TestCommunicationTemplate.ascx', 'Test Communication Template', 'Sends a test e-mail to the current user.', @BlockTypeGuid, 'Shepherd Church > Misc')
SET @BlockTypeId = (SELECT [Id] FROM [BlockType] WHERE [Guid] = @BlockTypeGuid)

-- Create the Communication Type attribute for the block type.
INSERT INTO [Attribute]
	([IsSystem], [FieldTypeId], [EntityTypeId], [EntityTypeQualifierColumn], [EntityTypeQualifierValue], [Key],	[Name], [Description], [Order], [IsGridColumn], [DefaultValue], [IsMultiValue], [IsRequired], [Guid], [AllowSearch])
	VALUES
	(0, @SingleSelectFieldTypeId, @BlockEntityTypeId, 'BlockTypeId', @BlockTypeId, 'CommunicationType', 'Communication Type', 'The type of test communications to use.', 0, 0, 'Template', 0, 1, @BlockAttributeCommunicationTypeGuid, 0)
SET @CommunicationTypeAttributeId = (SELECT [Id] FROM [Attribute] WHERE [Guid] = @BlockAttributeCommunicationTypeGuid)

-- Create the block instance on the System Emails page.
INSERT INTO [Block]
	([IsSystem], [PageId], [BlockTypeId], [Zone], [Order], [Name], [OutputCacheDuration], [Guid])
	VALUES
	(0, @SystemEmailsPageId, @BlockTypeId, 'Main', 10, 'Test System Email', 0, @BlockSystemEmailsGuid)
SET @BlockId = (SELECT [Id] FROM [Block] WHERE [Guid] = @BlockSystemEmailsGuid)

-- Set the block communication type to 'System'.
INSERT INTO [AttributeValue]
	([IsSystem], [AttributeId], [EntityId], [Value], [Guid])
	VALUES
	(0, @CommunicationTypeAttributeId, @BlockId, 'System', NEWID())

-- Create the block instance on the Communication Templates page.
INSERT INTO [Block]
	([IsSystem], [PageId], [BlockTypeId], [Zone], [Order], [Name], [OutputCacheDuration], [Guid])
	VALUES
	(0, @CommunicationTemplatesPageId, @BlockTypeId, 'Main', 10, 'Test Communication Template', 0, @BlockCommunicationTemplatesGuid)
SET @BlockId = (SELECT [Id] FROM [Block] WHERE [Guid] = @BlockCommunicationTemplatesGuid)

-- Set the block communication type to 'Template'
INSERT INTO [AttributeValue]
	([IsSystem], [AttributeId], [EntityId], [Value], [Guid])
	VALUES
	(0, @CommunicationTypeAttributeId, @BlockId, 'Template', NEWID())
