Imports System.IO
Imports System.Reflection
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Converters
Imports Newtonsoft.Json.Serialization

''' <summary>
''' A class which provides easy wrappers around JSON serialization.
''' </summary>
Public Module MySerialize

    'This JSON serialization thing requires global settings.... great. These are the settings.
    Private ReadOnly Property defaultSettings As New JsonSerializerSettings() With
    {
       .ContractResolver = New MyContractResolver(),
       .Formatting = Formatting.None,
       .ObjectCreationHandling = ObjectCreationHandling.Replace,
       .PreserveReferencesHandling = PreserveReferencesHandling.Objects
    }

    ''' <summary>
    ''' Save the given object as a JSON file with the given filename.
    ''' </summary>
    ''' <typeparam name="T"></typeparam>
    ''' <param name="filename"></param>
    ''' <param name="saveObject"></param>
    ''' <param name="expanded"></param>
    Public Sub SaveObject(Of T)(filename As String, saveObject As T, Optional expanded As Boolean = False)

        JsonConvert.DefaultSettings = Function() defaultSettings

        Using filestream As StreamWriter = File.CreateText(filename)

            Dim serializer = JsonSerializer.CreateDefault()

            'IDK, maybe people don't want formatted json. Maybe they're crazy.
            If (expanded) Then
                serializer.Formatting = Formatting.Indented
            Else
                serializer.Formatting = Formatting.None
            End If

            serializer.Serialize(filestream, saveObject)

        End Using

    End Sub

    ''' <summary>
    ''' Load an object from the given JSON file. 
    ''' </summary>
    ''' <typeparam name="T"></typeparam>
    ''' <param name="filename"></param>
    Public Function LoadObject(Of T)(filename As String) As T

        JsonConvert.DefaultSettings = Function() defaultSettings

        Dim newObject As T

        Using filestream As StreamReader = File.OpenText(filename)

            Dim serializer = JsonSerializer.CreateDefault()
            newObject = CType(serializer.Deserialize(filestream, GetType(T)), T)

        End Using

        Return newObject

    End Function

    'Taken from http//stackoverflow.com/questions/24106986/json-net-force-serialization-of-all-private-fields-And-all-fields-in-sub-classe
    Public Class MyContractResolver
        Inherits Newtonsoft.Json.Serialization.DefaultContractResolver

        Protected Overrides Function CreateContract(objectType As Type) As JsonContract

            Dim contract As JsonContract = MyBase.CreateContract(objectType)

            'This is just so that versions get automatically serialized correctly. Not sure why they're not already... honestly.
            If objectType = GetType(Version) Then
                contract.Converter = New VersionConverter()
            End If

            Return contract

        End Function

#Region "GarbageAttempts"
        'Protected Overrides Function CreateProperty(member As MemberInfo, memberSerialization As MemberSerialization) As JsonProperty

        '    Dim prop = MyBase.CreateProperty(member, memberSerialization)
        '    prop.Writable = ShouldSerialize(member, True) 'CanSetMemberValue(member, True)
        '    prop.Readable = ShouldSerialize(member, True) 'CanReadMemberValue(member, True)
        '    prop.Ignored = Not ShouldSerialize(member, True)
        '    Return prop

        'End Function

        'Private Function ShouldSerialize(Member As MemberInfo, NonPublic As Boolean) As Boolean

        '    'Return True
        '    Select Case (Member.MemberType)
        '        Case MemberTypes.Field 'ALL fields should be serialized.
        '            Return True
        '            'Dim fInfo = CType(Member, FieldInfo)
        '            'Return NonPublic Or fInfo.IsPublic
        '        Case MemberTypes.Property
        '            Dim pInfo = CType(Member, PropertyInfo)
        '            Return pInfo.GetMethod = Nothing OrElse pInfo.SetMethod <> Nothing
        '            'If Not PropertyInfo.CanWrite Then Return False
        '            'If NonPublic Then Return True
        '            'Return PropertyInfo.GetSetMethod(NonPublic) <> Nothing
        '        Case Else
        '            Return False
        '    End Select

        'End Function

        'Private Function CanSetMemberValue(Member As MemberInfo, NonPublic As Boolean) As Boolean

        '    Select Case (Member.MemberType)
        '        Case MemberTypes.Field
        '            Dim FieldInfo = CType(Member, FieldInfo)
        '            Return NonPublic Or FieldInfo.IsPublic
        '        Case MemberTypes.Property
        '            Dim PropertyInfo = CType(Member, PropertyInfo)
        '            If Not PropertyInfo.CanWrite Then Return False
        '            If NonPublic Then Return True
        '            Return PropertyInfo.GetSetMethod(NonPublic) <> Nothing
        '        Case Else
        '            Return False
        '    End Select

        'End Function

        'Protected Overrides Function CreateProperties(type As Type, memberSerialization As MemberSerialization) As IList(Of JsonProperty)

        '    Dim props = MyBase.CreateProperties(type, memberSerialization)

        '    'SERIALIZE EVERYTHING OMG
        '    'For Each prop In props
        '    '    prop.Ignored = False
        '    '    prop.Readable = True
        '    'Next

        '    'Everything that is NOT readonly should not be ignored when serializing
        '    'For Each prop In props.Where(Function(x) Not (x.Readable And Not x.Writable))
        '    '    prop.Ignored = False
        '    'Next

        '    Return props

        'End Function

#End Region

        Protected Overrides Function CreateProperties(type As Type, memberSerialization As MemberSerialization) As IList(Of JsonProperty)

            'Dim properties = type.GetProperties(BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance).Select(
            '   Function(p) MyBase.CreateProperty(p, memberSerialization))

            Dim currentType = type
            Dim fields As New List(Of JsonProperty)

            'Walk inheritance tree to get ALL values in ALL types. Yeah
            While currentType IsNot Nothing
                fields.AddRange(currentType.GetFields(BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance).Select(
                        Function(f) MyBase.CreateProperty(f, memberSerialization)))
                currentType = currentType.BaseType
            End While

            Dim props = fields.ToList() 'properties.Union(fields).ToList()

            props.ForEach(Sub(p)
                              p.Writable = True
                              p.Readable = True
                          End Sub)
            Return props

        End Function

    End Class

End Module
