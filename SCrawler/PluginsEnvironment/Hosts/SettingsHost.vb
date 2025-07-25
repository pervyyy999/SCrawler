﻿' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Imports System.Reflection
Imports SCrawler.API.Base
Imports SCrawler.API.YouTube.Objects
Imports SCrawler.Plugin.Attributes
Imports PersonalUtilities.Functions.XML
Imports PersonalUtilities.Functions.XML.Base
Imports PersonalUtilities.Functions.XML.Objects
Imports PersonalUtilities.Tools
Imports PersonalUtilities.Tools.Web.Clients
Imports Download = SCrawler.Plugin.ISiteSettings.Download
Namespace Plugin.Hosts
    Friend Class SettingsHost : Implements IIndexable, IEquatable(Of SettingsHost), IDisposableSuspend
#Region "Events"
        Friend Delegate Sub SettingsHostActionEventHandler(ByVal Obj As SettingsHost)
        Friend Event Deleted As SettingsHostActionEventHandler
        Friend Event OkClick As SettingsHostActionEventHandler
        Friend Event CloneClick As SettingsHostActionEventHandler
#End Region
#Region "Controls"
        Private WithEvents BTT_SETTINGS As ToolStripMenuItem
        Private WithEvents BTT_SETTINGS_ACTIONS_EDIT As ToolStripMenuItem
        Private WithEvents BTT_SETTINGS_ACTIONS_DELETE As ToolStripMenuItem
        Private WithEvents BTT_SETTINGS_ACTIONS_CLONE As ToolStripMenuItem
        Private WithEvents BTT_SETTINGS_INTERNAL As Button
        Private ReadOnly SpecialFormAttribute As SpecialForm = Nothing
#Region "Menu buttons"
        Friend Function GetSettingsButton() As ToolStripItem
            UpdateSettingsButton()
            Return BTT_SETTINGS
        End Function
        Private Sub UpdateSettingsButton()
            If BTT_SETTINGS Is Nothing Then
                BTT_SETTINGS = New ToolStripMenuItem
                If [Default] Then
                    BTT_SETTINGS.Text = NameAccountNameDefault
                    If Not _AccountName.IsEmptyString Then BTT_SETTINGS.Text &= $" [{_AccountName}]"
                Else
                    BTT_SETTINGS.Text = AccountName
                End If
                If Not Source.Image Is Nothing Then BTT_SETTINGS.Image = Source.Image
            End If
            If BTT_SETTINGS_ACTIONS_EDIT Is Nothing Then BTT_SETTINGS_ACTIONS_EDIT = New ToolStripMenuItem("Edit", My.Resources.PencilPic_16) With {.BackColor = MyColor.EditBack, .ForeColor = MyColor.EditFore}
            If BTT_SETTINGS_ACTIONS_DELETE Is Nothing Then BTT_SETTINGS_ACTIONS_DELETE = New ToolStripMenuItem("Delete", My.Resources.DeletePic_24) With {.BackColor = MyColor.DeleteBack, .ForeColor = MyColor.DeleteFore}
            If BTT_SETTINGS_ACTIONS_CLONE Is Nothing Then BTT_SETTINGS_ACTIONS_CLONE = New ToolStripMenuItem("Clone", My.Resources.PlusPic_24) With {.BackColor = MyColor.OkBack, .ForeColor = MyColor.OkFore}
            If BTT_SETTINGS.DropDownItems.Count = 0 Then BTT_SETTINGS.DropDownItems.AddRange({BTT_SETTINGS_ACTIONS_EDIT, BTT_SETTINGS_ACTIONS_DELETE, BTT_SETTINGS_ACTIONS_CLONE})
        End Sub
#End Region
#Region "Editor form button"
        Friend Function GetSettingsButtonInternal() As Button
            If Not SpecialFormAttribute Is Nothing AndAlso SpecialFormAttribute.SettingsForm Then
                BTT_SETTINGS_INTERNAL = New Button With {.Text = "Other settings", .Dock = DockStyle.Right, .Width = 150}
                Return BTT_SETTINGS_INTERNAL
            Else
                Return Nothing
            End If
        End Function
#End Region
#Region "Menu buttons handlers"
        Friend Sub SettingsPerformClick() Handles BTT_SETTINGS.Click, BTT_SETTINGS_ACTIONS_EDIT.Click
            Try
                Using f As New Editors.SiteEditorForm(Me)
                    f.ShowDialog()
                    If f.DialogResult = DialogResult.OK And IsAbstract Then RaiseEvent OkClick(Me)
                End Using
            Catch ex As Exception
                ErrorsDescriber.Execute(EDP.LogMessageValue, ex, "[Plugin.Hosts.SettingsHost.OpenSettingsForm]")
            End Try
        End Sub
        Private Sub BTT_SETTINGS_ACTIONS_DELETE_Click(sender As Object, e As EventArgs) Handles BTT_SETTINGS_ACTIONS_DELETE.Click
            Const msgTitle$ = "Deleting a profile"
            If Downloader.Working Then
                MsgBoxE({"You cannot delete a profile until the download is complete", msgTitle}, vbCritical)
            ElseIf [Default] Then
                MsgBoxE({"The default profile cannot be deleted", msgTitle}, vbCritical)
            ElseIf MsgBoxE({$"Are you sure you want to delete this profile ({AccountName})?", msgTitle}, vbExclamation,,, {"Process", "Cancel"}) = 0 Then
                RaiseEvent Deleted(Me)
            End If
        End Sub
        Private Sub BTT_SETTINGS_ACTIONS_CLONE_Click(sender As Object, e As EventArgs) Handles BTT_SETTINGS_ACTIONS_CLONE.Click
            RaiseEvent CloneClick(Me)
        End Sub
#End Region
#Region "Editor form buttons handlers"
        Private Sub BTT_SETTINGS_INTERNAL_Click(sender As Object, e As EventArgs) Handles BTT_SETTINGS_INTERNAL.Click
            Try
                If Not SpecialFormAttribute Is Nothing AndAlso SpecialFormAttribute.SettingsForm Then Source.OpenSettingsForm()
            Catch ex As Exception
                ErrorsDescriber.Execute(EDP.LogMessageValue, ex, "[Plugin.Hosts.SettingsHost.OpenSettingsForm(Button)]")
            End Try
        End Sub
#End Region
#End Region
#Region "IIndexable Support"
        Friend Property Index As Integer = -1 Implements IIndexable.Index
        Private Function SetIndex(ByVal Obj As Object, ByVal Index As Integer) As Object Implements IIndexable.SetIndex
            DirectCast(Obj, SettingsHost).Index = Index
            Return Obj
        End Function
#End Region
#Region "Host declarations"
        Friend ReadOnly Property Source As ISiteSettings
        Friend ReadOnly Property PropList As List(Of PropertyValueHost)
        Friend ReadOnly Property Name As String
            Get
                Return Source.Site
            End Get
        End Property
        Private ReadOnly _Key As String
        Friend ReadOnly Property Key As String
            Get
                Return _Key.IfNullOrEmpty(Name)
            End Get
        End Property
        Friend ReadOnly Property KeyDownloader As String
            Get
                Return $"{Key}_{AccountName}"
            End Get
        End Property
        Friend Const NameAccountNameDefault As String = "Default"
        Friend Const NameXML_AccountName As String = "AccountName"
        Private ReadOnly _AccountName As XMLValue(Of String)
        Friend Property AccountName As String
            Get
                If Not _AccountName.IsEmptyString Then
                    Return _AccountName
                ElseIf Index = 0 Then
                    Return NameAccountNameDefault
                Else
                    Return String.Empty
                End If
            End Get
            Set(ByVal _AccountName As String)
                Me._AccountName.Value = _AccountName
                Me.Source.AccountName = _AccountName
            End Set
        End Property
        Friend ReadOnly Property [Default] As Boolean
        Friend ReadOnly Property IsSeparatedTasks As Boolean = False
        Friend ReadOnly Property IsSavedPostsCompatible As Boolean = False
        Private ReadOnly _TaskCountDefined As Integer? = Nothing
        Friend ReadOnly Property TaskCount As Integer
            Get
                If IsSeparatedTasks Then
                    If PropList.Count > 0 Then
                        Dim i% = PropList.FindIndex(Function(p) p.IsTaskCounter)
                        If i >= 0 Then Return CInt(PropList(i).Value)
                    End If
                    If _TaskCountDefined.HasValue AndAlso _TaskCountDefined.Value > 0 Then Return _TaskCountDefined.Value
                End If
                Return Settings.MaxUsersJobsCount
            End Get
        End Property
        Friend ReadOnly Property TaskGroupName As String = String.Empty
        Friend ReadOnly Property HasSpecialOptions As Boolean = False
        Private ReadOnly _ResponserGetMethod As MethodInfo
        Private ReadOnly _ResponserIsContainer As Boolean = False
        Friend ReadOnly Property Responser As Responser
            Get
                If Not _ResponserGetMethod Is Nothing Then
                    Return _ResponserGetMethod.Invoke(Source, Nothing)
                ElseIf _ResponserIsContainer Then
                    Return DirectCast(Source, IResponserContainer).Responser
                Else
                    Return Nothing
                End If
            End Get
        End Property
#End Region
#Region "Base properties compatibility"
        Friend ReadOnly Property DownloadSiteData As XMLValue(Of Boolean)
        Friend ReadOnly Property Temporary As XMLValue(Of Boolean)
        Friend ReadOnly Property DownloadImages As XMLValue(Of Boolean)
        Friend ReadOnly Property DownloadVideos As XMLValue(Of Boolean)
        Private ReadOnly _Path As XMLValue(Of SFile)
        Friend Property Path(Optional ByVal SetProp As Boolean = True) As SFile
            Get
                If _Path.IsEmptyString Then
                    Dim tmpPath As SFile = SFile.GetPath($"{Settings.GlobalPath.Value.PathWithSeparator}{Source.Site}")
                    If SetProp Then _Path.Value = tmpPath Else Return tmpPath
                End If
                Return _Path.Value
            End Get
            Set(ByVal NewPath As SFile)
                _Path.Value = NewPath
            End Set
        End Property
        Friend Const SavedPostsFolderName As String = "!Saved"
        Private ReadOnly _SavedPostsPath As XMLValue(Of SFile)
        Friend Property SavedPostsPath(Optional ByVal GetAny As Boolean = True) As SFile
            Get
                If Not _SavedPostsPath.Value.IsEmptyString Then
                    Return _SavedPostsPath.Value
                Else
                    If GetAny Then
                        Return $"{Path.PathNoSeparator}\{SavedPostsFolderName}\"
                    Else
                        Return Nothing
                    End If
                End If
            End Get
            Set(ByVal NewPath As SFile)
                _SavedPostsPath.Value = NewPath
            End Set
        End Property
        Friend Property DownloadSavedPosts As XMLValue(Of Boolean)
        Friend ReadOnly Property GetUserMediaOnly As XMLValue(Of Boolean)
#End Region
        Friend ReadOnly Property IsAbstract As Boolean = False
        Friend Sub New(ByVal Plugin As ISiteSettings, ByVal GlobalPath As SFile, ByVal _Temp As Boolean, _Imgs As Boolean, _Vids As Boolean)
            Me.New(Plugin, False, Nothing, GlobalPath, _Temp, _Imgs, _Vids)
        End Sub
        Friend Sub New(ByVal Plugin As ISiteSettings, ByVal IsDef As Boolean, ByRef _XML As XmlFile, ByVal GlobalPath As SFile,
                       ByRef _Temp As XMLValue(Of Boolean), ByRef _Imgs As XMLValue(Of Boolean), ByRef _Vids As XMLValue(Of Boolean))
            Source = Plugin
            Source.Logger = LogConnector
            [Default] = IsDef
            If _XML Is Nothing Then IsAbstract = True

            PropList = New List(Of PropertyValueHost)

            Dim ClsAttr As IEnumerable(Of Attribute) = Source.GetType.GetCustomAttributes
            If ClsAttr.ListExists Then
                For Each a As Attribute In ClsAttr
                    If TypeOf a Is Manifest Then
                        _Key = DirectCast(a, Manifest).GUID
                    ElseIf TypeOf a Is SeparatedTasks Then
                        IsSeparatedTasks = True
                        With DirectCast(a, SeparatedTasks)
                            If .TasksCount > 0 Then _TaskCountDefined = .TasksCount
                        End With
                    ElseIf TypeOf a Is TaskGroup Then
                        TaskGroupName = DirectCast(a, TaskGroup).Name
                    ElseIf TypeOf a Is SavedPosts Then
                        IsSavedPostsCompatible = True
                    ElseIf TypeOf a Is SpecialForm Then
                        With DirectCast(a, SpecialForm)
                            If .SettingsForm Then
                                SpecialFormAttribute = a
                            Else
                                HasSpecialOptions = True
                            End If
                        End With
                    End If
                Next
            End If

            Dim i%
            Dim n() As String = {SettingsCLS.Name_Node_Sites, Name}

            _AccountName = New XMLValue(Of String)(NameXML_AccountName,, _XML, n)
            Source.AccountName = _AccountName

            Source.BeginInit()

            Dim Members As IEnumerable(Of MemberInfo) = GetObjectMembers(Plugin,,, True, New MembersDistinctComparerExtended) 'Plugin.GetType.GetTypeInfo.DeclaredMembers

            _ResponserIsContainer = TypeOf Plugin Is IResponserContainer
            If Members.ListExists Then
                Dim Updaters As New List(Of MemberInfo)
                Dim Providers As New List(Of MemberInfo)
                Dim PropCheckers As New List(Of MemberInfo)

                Dim m As MemberInfo
                For Each m In Members
                    If m.MemberType = MemberTypes.Property Then
                        PropList.Add(New PropertyValueHost(Me, Source, m))
                        With DirectCast(m, PropertyInfo)
                            If .PropertyType Is GetType(Responser) AndAlso m.GetCustomAttribute(Of DoNotUse)() Is Nothing Then _ResponserGetMethod = .GetMethod
                        End With
                    End If
                    With m.GetCustomAttributes()
                        If .ListExists Then
                            If m.MemberType = MemberTypes.Method Then
                                For i = 0 To .Count - 1
                                    If .ElementAt(i).GetType Is GetType(PropertyUpdater) Then
                                        Updaters.Add(m)
                                    ElseIf .ElementAt(i).GetType Is GetType(PropertiesDataChecker) Then
                                        PropCheckers.Add(m)
                                    End If
                                Next
                            ElseIf m.MemberType = MemberTypes.Property Then
                                If m.GetCustomAttributes(Of Provider)().ListExists Then Providers.Add(m)
                            End If
                        End If
                    End With
                Next
                PropList.RemoveAll(Function(p) Not p.Exists)
                If Updaters.Count > 0 Then
                    Dim up As PropertyUpdater
                    For Each m In Updaters
                        up = m.GetCustomAttribute(Of PropertyUpdater)()
                        i = PropList.FindIndex(Function(p) p.Name = up.Name)
                        If i >= 0 Then PropList(i).SetUpdateMethod(DirectCast(m, MethodInfo), up.Arguments)
                    Next
                    Updaters.Clear()
                End If
                If Providers.Count > 0 Then
                    Dim prov As IEnumerable(Of Provider)
                    Dim _prov As Provider
                    For Each m In Providers
                        prov = m.GetCustomAttributes(Of Provider)()
                        If prov.ListExists Then
                            For Each _prov In prov
                                i = PropList.FindIndex(Function(p) p.Name = _prov.Name)
                                If i >= 0 Then PropList(i).SetProvider(DirectCast(DirectCast(m, PropertyInfo).GetValue(Source), IFormatProvider), _prov)
                            Next
                        End If
                    Next
                    Providers.Clear()
                End If
                If PropCheckers.Count > 0 Then
                    Dim pc As PropertiesDataChecker
                    For Each m In PropCheckers
                        pc = m.GetCustomAttribute(Of PropertiesDataChecker)()
                        If pc.ComparableNames.ListExists Then
                            i = PropList.FindIndex(Function(p) p.Name = pc.ComparableNames(0))
                            If i >= 0 Then
                                With PropList(i)
                                    .PropertiesChecking = pc.ComparableNames
                                    .PropertiesCheckingMethod = DirectCast(m, MethodInfo)
                                End With
                            End If
                        End If
                    Next
                    PropCheckers.Clear()
                End If
                PropList.ForEach(Sub(p) p.SetDependents(PropList))
            End If

            _Path = New XMLValue(Of SFile)("Path",, _XML, n, New XMLToFilePathProvider)
            _SavedPostsPath = New XMLValue(Of SFile)("SavedPostsPath",, _XML, n, New XMLToFilePathProvider)
            DownloadSavedPosts = New XMLValue(Of Boolean)("DownloadSavedPosts", True, _XML, n)

            Temporary = New XMLValue(Of Boolean)
            Temporary.SetExtended("Temporary", False, _XML, n)
            Temporary.SetDefault(_Temp)

            DownloadImages = New XMLValue(Of Boolean)
            DownloadImages.SetExtended("DownloadImages", True, _XML, n)
            DownloadImages.SetDefault(_Imgs)

            DownloadVideos = New XMLValue(Of Boolean)
            DownloadVideos.SetExtended("DownloadVideos", True, _XML, n)
            DownloadVideos.SetDefault(_Vids)

            DownloadSiteData = New XMLValue(Of Boolean)("DownloadSiteData", True, _XML, n)

            GetUserMediaOnly = New XMLValue(Of Boolean)("GetUserMediaOnly", True, _XML, n)
            If PropList.Count > 0 Then
                Dim MaxOffset% = Math.Max(PropList.Max(Function(pp) pp.LeftOffset), PropertyValueHost.LeftOffsetDefault)
                For Each p As PropertyValueHost In PropList
                    If Not IsAbstract Then p.SetXmlEnvironment(_XML, n)
                    p.LeftOffset = MaxOffset
                Next
            End If

            Source.EndInit()
        End Sub
        Friend Function Apply(ByVal _XML As XmlFile, ByVal GlobalPath As SFile,
                              ByRef _Temp As XMLValue(Of Boolean), ByRef _Imgs As XMLValue(Of Boolean), ByRef _Vids As XMLValue(Of Boolean)) As SettingsHost
            Dim newHost As New SettingsHost(Source.Clone(True), False, _XML, GlobalPath, _Temp, _Imgs, _Vids)

            _XML.BeginUpdate()

            With newHost
                ._AccountName.Value = _AccountName.Value
                .DownloadSiteData.Value = DownloadSiteData.Value
                .Temporary.ValueF = Temporary.ValueF
                .DownloadImages.ValueF = DownloadImages.ValueF
                .DownloadVideos.ValueF = DownloadVideos.ValueF
                ._Path.Value = _Path.Value
                ._SavedPostsPath.Value = _SavedPostsPath.Value
                .DownloadSavedPosts.Value = .DownloadSavedPosts.Value
                .GetUserMediaOnly.Value = .GetUserMediaOnly.Value
                With .Source
                    .BeginInit()
                    .AccountName = _AccountName
                    .Update(Source)
                    .AccountName = _AccountName
                    .EndInit()
                End With
            End With

            _XML.EndUpdate()

            Return newHost
        End Function
        Friend Function Clone() As SettingsHost
            Return New SettingsHost(Source.Clone(False), Path, Temporary, DownloadImages, DownloadVideos) With {.SavedPostsPath = SavedPostsPath, .Path = Path}
        End Function
#Region "Forks"
        Friend Function IsMyUser(ByVal UserURL As String) As ExchangeOptions
            Dim s As ExchangeOptions = Source.IsMyUser(UserURL)
            If Not s.UserName.IsEmptyString Then s.HostKey = Key
            Return s
        End Function
        Friend Function IsMyImageVideo(ByVal URL As String) As ExchangeOptions
            Dim s As ExchangeOptions = Source.IsMyImageVideo(URL)
            If s.Exists Then s.SiteName = Name : s.HostKey = Key
            Return s
        End Function
        Friend Function GetInstance(ByVal What As Download, ByVal u As UserInfo, Optional ByVal _LoadUserInformation As Boolean = True,
                                    Optional ByVal AttachUserInfo As Boolean = True) As IUserData
            Dim p As IPluginContentProvider = Source.GetInstance(What)
            If Not p Is Nothing Then
                Dim pp As IUserData
                If TypeOf p Is IUserData Then pp = p Else pp = New UserDataHost(p)
                pp.SetEnvironment(Me, u, _LoadUserInformation, AttachUserInfo)
                Return pp
            Else
                Throw New ArgumentNullException("IPluginContentProvider", $"Plugin [{Key}] does not provide user instance")
            End If
        End Function
        Friend Function GetSingleMediaInstance(ByVal URL As String, ByVal OutputFile As SFile) As IYouTubeMediaContainer
            Dim m As IDownloadableMedia = Source.GetSingleMediaInstance(URL, OutputFile)
            If Not m Is Nothing Then
                If Not TypeOf m Is YouTubeMediaContainerBase Then m = New DownloadableMediaHost(m)
                m.Instance = GetInstance(Download.SingleObject, Nothing, False, False)
                With DirectCast(m, YouTubeMediaContainerBase)
                    If Not Source.Image Is Nothing Then
                        .SiteIcon = Source.Image
                    ElseIf Not Source.Icon Is Nothing Then
                        .SiteIcon = Source.Icon.ToBitmap
                    Else
                        .SiteIcon = Nothing
                    End If
                    .SiteKey = Key
                    .Site = Name
                End With
                Return m
            Else
                Throw New ArgumentNullException("IDownloadableMedia", $"Plugin [{Key}] does not provide IDownloadableMedia")
            End If
        End Function
        Friend Function GetUserPostUrl(ByVal User As IPluginContentProvider, ByVal Media As IUserMedia) As String
            Return Source.GetUserPostUrl(User, Media)
        End Function
        Private _AvailableValue As Boolean = True
        Private _AvailableAsked As Boolean = False
        Private _ActiveTaskCount As Integer = 0
        Friend Property AvailableText As String = String.Empty
        Friend Function Available(ByVal What As Download, ByVal Silent As Boolean) As Boolean
            If DownloadSiteData Then
                If Not _AvailableAsked Then
                    _AvailableValue = Source.Available(What, Silent)
                    AvailableText = Source.AvailableText
                    _AvailableAsked = True
                End If
                Return _AvailableValue
            Else
                AvailableText = $"Downloading data for the site {Name} - {AccountName.IfNullOrEmpty(NameAccountNameDefault)} has been disabled by you."
                If Not Silent Then MsgBoxE({AvailableText, $"{Name} downloading disabled"}, vbExclamation)
                Return False
            End If
        End Function
        Friend Sub DownloadStarted(ByVal What As Download)
            _ActiveTaskCount += 1
            Source.DownloadStarted(What)
        End Sub
        Friend Sub BeforeStartDownload(ByVal User As IUserData, ByVal What As Download)
            Source.BeforeStartDownload(ConvertUser(User), What)
        End Sub
        Friend Sub AfterDownload(ByVal User As IUserData, ByVal What As Download)
            Source.AfterDownload(ConvertUser(User), What)
        End Sub
        Friend Sub DownloadDone(ByVal What As Download)
            _ActiveTaskCount -= 1
            If _ActiveTaskCount = 0 Then _AvailableAsked = False : AvailableText = String.Empty
            Source.DownloadDone(What)
        End Sub
        Private Function ConvertUser(ByVal User As IUserData) As Object
            Return If(DirectCast(User, UserDataBase).ExternalPlugin, User)
        End Function
#End Region
#Region "IEquatable Support"
        Friend Overloads Function Equals(ByVal Other As SettingsHost) As Boolean Implements IEquatable(Of SettingsHost).Equals
            Return [Default] = Other.Default OrElse AccountName = Other.AccountName
        End Function
        Public Overrides Function Equals(ByVal Obj As Object) As Boolean
            Return Equals(DirectCast(Obj, SettingsHost))
        End Function
#End Region
#Region "IDisposable Support"
        Private disposedValue As Boolean = False
        Friend Property DisposeSuspended As Boolean Implements IDisposableSuspend.DisposeSuspended
            Get
                Return [Default]
            End Get
            Private Set : End Set
        End Property
        Friend ReadOnly Property Disposed As Boolean Implements IDisposableSuspend.Disposed
            Get
                Return disposedValue
            End Get
        End Property
        Protected Overridable Overloads Sub Dispose(ByVal disposing As Boolean)
            If Not disposedValue And Not DisposeSuspended Then
                If disposing Then
                    Source.DisposeIfReady
                    If Not BTT_SETTINGS Is Nothing Then BTT_SETTINGS.DropDownItems.Clear()
                    BTT_SETTINGS.DisposeIfReady
                    BTT_SETTINGS_ACTIONS_EDIT.DisposeIfReady
                    BTT_SETTINGS_ACTIONS_DELETE.DisposeIfReady
                    BTT_SETTINGS_ACTIONS_CLONE.DisposeIfReady
                    BTT_SETTINGS_INTERNAL.DisposeIfReady
                    PropList.ListClearDispose
                    XMLValuesDispose(Me, False)
                End If
                BTT_SETTINGS = Nothing
                BTT_SETTINGS_INTERNAL = Nothing
                disposedValue = True
            End If
        End Sub
        Protected Overrides Sub Finalize()
            Dispose(False)
            MyBase.Finalize()
        End Sub
        Friend Overloads Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
#End Region
    End Class
End Namespace