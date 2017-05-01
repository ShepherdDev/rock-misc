DECLARE @BlockTypeGuid AS UNIQUEIDENTIFIER = '8988971D-6B8F-4759-ABD4-F44EAD3F879F'
DECLARE @BlockTypeId AS INT = (SELECT [Id] FROM [BlockType] WHERE [Guid] = @BlockTypeGuid)
DECLARE @BlockAttributeCommunicationTypeGuid AS UNIQUEIDENTIFIER = 'B85DB007-5AC9-4458-9F68-05807408241C'
DECLARE @CommunicationTypeAttributeId AS INT = (SELECT [Id] FROM [Attribute] WHERE [Guid] = @BlockAttributeCommunicationTypeGuid)

DELETE FROM [AttributeValue]
	WHERE [AttributeId] = @CommunicationTypeAttributeId

DELETE FROM [Attribute]
	WHERE [Id] = @CommunicationTypeAttributeId

DELETE FROM [Block]
	WHERE [BlockTypeId] = @BlockTypeId

DELETE FROM [BlockType]
	WHERE [Id] = @BlockTypeId
