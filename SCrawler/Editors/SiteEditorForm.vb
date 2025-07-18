﻿' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Imports SCrawler.Plugin
Imports SCrawler.Plugin.Hosts
Imports PersonalUtilities.Forms
Imports PersonalUtilities.Forms.Controls
Imports PersonalUtilities.Forms.Controls.Base
Imports PersonalUtilities.Tools.Web.Cookies
Imports ADB = PersonalUtilities.Forms.Controls.Base.ActionButton.DefaultButtons
Namespace Editors
    Friend Class SiteEditorForm
        Private ReadOnly LBL_AUTH As Label
        Private ReadOnly LBL_OTHER As Label
        Private WithEvents MyDefs As DefaultFormOptions
        Private WithEvents SpecialButton As Button
        Private Property Cookies As CookieKeeper
        Private CookiesChanged As Boolean = False
#Region "Providers"
        Private Class SavedPostsChecker : Inherits AccountsNameChecker
            Friend ReadOnly PathControl As TextBoxExtended
            Friend ReadOnly HostPaths As List(Of String)
            Friend Shared Function GetNewSavedPostsName(ByVal PathText As String, ByVal HostIndex As Integer, ByVal MaxCount As Integer) As String
                Return If(Not PathText.IsEmptyString, $"{PathText.CSFilePS}{SettingsHost.SavedPostsFolderName}{If(HostIndex = 0, String.Empty, $"_{IIf(HostIndex = -1, MaxCount + 1, HostIndex)}")}\", String.Empty)
            End Function
            Friend Sub New(ByVal h As SettingsHostCollection, ByVal i As Integer)
                MyBase.New(h, i)
                HostPaths = New List(Of String)(h.Select(Function(hh) hh.Path.PathNoSeparator))
            End Sub
            Friend Sub New(ByVal h As SettingsHostCollection, ByVal i As Integer, ByRef _PathControl As TextBoxExtended)
                Me.New(h, i)
                PathControl = _PathControl
            End Sub
            Public Overrides Function Convert(ByVal Value As Object, ByVal DestinationType As Type, ByVal Provider As IFormatProvider,
                                              Optional ByVal NothingArg As Object = Nothing, Optional ByVal e As ErrorsDescriber = Nothing) As Object
                Dim v$ = AConvert(Of String)(Value, String.Empty)
                If Not v.IsEmptyString AndAlso v.Contains("/") Then
                    ErrorMessage = $"Path [{Name}] contains forbidden character ""/"""
                    HasError = True
                ElseIf HostCollection.Count = 1 And HostIndex = 0 Then
                    Return Value
                ElseIf v.IsEmptyString Then
                    If Not PathControl.IsEmptyString AndAlso Not HostPaths.Contains(PathControl.Text.CSFilePSN) Then
                        Return GetNewSavedPostsName(PathControl.Text, HostIndex, HostCollection.Count)
                    Else
                        HasError = True
                        ErrorMessage = "The path to saved posts is not unique!"
                    End If
                Else
                    Return Value
                End If
                Return Nothing
            End Function
        End Class
        Private Class SavedPostsControlParams : Inherits FieldsChecker.ControlParams
            Private ReadOnly Property ProviderE As SavedPostsChecker
                Get
                    Return Provider
                End Get
            End Property
            Public Overrides Property CanBeNull As Boolean
                Get
                    With ProviderE
                        If .HostCollection.Count = 1 And .HostIndex = 0 Then
                            Return True
                        ElseIf Not .PathControl.IsEmptyString Then
                            Dim v$ = .PathControl.Text.CSFilePSN
                            Return Not .HostPaths.Contains(v)
                        Else
                            Return False
                        End If
                    End With
                End Get
                Set : End Set
            End Property
            Friend Sub New(ByVal Name As String, ByRef RelatedControl As TextBoxExtended, ByRef _PathControl As TextBoxExtended,
                           ByVal h As SettingsHostCollection, ByVal i As Integer)
                MyBase.New(Name, GetType(String), RelatedControl, True, New SavedPostsChecker(h, i, _PathControl))
            End Sub
            Public Overrides Function Validate() As Boolean
                If MyBase.Validate() Then
                    Return True
                Else
                    DirectCast(WorkingControl, TextBoxExtended).Text = ProviderE.Convert(Validate_GetValue, GetType(String), Nothing, String.Empty, EDP.ReturnValue)
                    Return MyBase.Validate()
                End If
            End Function
        End Class
        Private Class AccountsNameChecker : Inherits FieldsCheckerProviderBase
            Friend ReadOnly HostCollection As SettingsHostCollection
            Friend ReadOnly HostIndex As Integer
            Public Overrides Property ErrorMessage As String
                Get
                    Return MyBase.ErrorMessage
                End Get
                Set(ByVal m As String)
                    MyBase.ErrorMessage = m
                    HasError = True
                End Set
            End Property
            Public Overrides Sub Reset()
                MyBase.Reset()
                MyBase.ErrorMessage = String.Empty
            End Sub
            Friend Sub New(ByVal h As SettingsHostCollection, ByVal i As Integer)
                HostCollection = h
                HostIndex = i
            End Sub
            Public Overrides Function Convert(ByVal Value As Object, ByVal DestinationType As Type, ByVal Provider As IFormatProvider,
                                              Optional ByVal NothingArg As Object = Nothing, Optional ByVal e As ErrorsDescriber = Nothing) As Object
                If HostCollection.Count = 1 And HostIndex = 0 Then
                    Return Value
                Else
                    Dim v$ = AConvert(Of String)(Value, String.Empty)
                    If v.IsEmptyString Then
                        If HostIndex = 0 Then
                            Return v
                        Else
                            ErrorMessage = "Settings name cannot be null"
                        End If
                    ElseIf v = SettingsHost.NameAccountNameDefault Then
                        If HostIndex = 0 Then
                            Return v
                        Else
                            ErrorMessage = "The default name can only be set for the first settings!"
                        End If
                    Else
                        Dim i% = HostCollection.IndexOf(v)
                        If i >= 0 And i <> HostIndex Then ErrorMessage = $"There are already settings named '{v}'" Else Return v
                    End If
                End If
                Return Nothing
            End Function
        End Class
#End Region
        Private ReadOnly Property Host As SettingsHost
        Private Property HostCollection As SettingsHostCollection
        Friend Sub New(ByVal h As SettingsHost)
            InitializeComponent()
            MyDefs = New DefaultFormOptions(Me, Settings.Design)
            Host = h
            If Not Host.Responser Is Nothing Then Cookies = Host.Responser.Cookies.Copy
            LBL_AUTH = New Label With {.Text = "Authorization", .TextAlign = ContentAlignment.MiddleCenter, .Dock = DockStyle.Fill}
            LBL_OTHER = New Label With {.Text = "Other Parameters", .TextAlign = ContentAlignment.MiddleCenter, .Dock = DockStyle.Fill}
            Host.Source.BeginEdit()
        End Sub
        Private Sub SiteEditorForm_Load(sender As Object, e As EventArgs) Handles Me.Load
            Try
                With MyDefs
                    .MyViewInitialize(True)
                    .AddOkCancelToolbar()

                    .MyFieldsChecker = New FieldsChecker
                    With Host
                        HostCollection = Settings(Host.Key)
                        With .Source
                            Text = .Site
                            If Not .Icon Is Nothing Then Icon = .Icon Else ShowIcon = False
                        End With
                        If Not .AccountName.IsEmptyString Then Text &= $" [{ .AccountName}]"
                        If hostCollection.Count = 1 Then TXT_PATH_SAVED_POSTS.Button(ADB.Refresh).Visible = False

                        SetCookieText()

                        TXT_PATH_SAVED_POSTS.Text = .SavedPostsPath(False)
                        _PathBefore = .Path(False)
                        TXT_PATH.Text = _PathBefore
                        TXT_ACCOUNT_NAME.Text = .AccountName
                        If Host.Index = 0 Then TXT_PATH_SAVED_POSTS.Button(ADB.Refresh).Visible = False : TXT_PATH_SAVED_POSTS.Buttons.UpdateButtonsPositions()
                        If Host.Index >= 0 Then TXT_ACCOUNT_NAME.Enabled = False : TXT_ACCOUNT_NAME.Buttons.Clear() : TXT_ACCOUNT_NAME.Buttons.UpdateButtonsPositions()
                        CH_DOWNLOAD_SITE_DATA.Checked = .DownloadSiteData
                        CH_USE_SAVED_POSTS.Checked = .DownloadSavedPosts
                        CH_GET_USER_MEDIA_ONLY.Checked = .GetUserMediaOnly

                        SiteDefaultsFunctions.SetChecker(TP_SITE_PROPS, Host)

                        With MyDefs.MyFieldsCheckerE
                            .AddControl(Of String)(TXT_PATH, TXT_PATH.CaptionText, True, New SavedPostsChecker(HostCollection, Host.Index))
                            .AddControl(New SavedPostsControlParams(TXT_PATH_SAVED_POSTS.CaptionText, TXT_PATH_SAVED_POSTS, TXT_PATH, HostCollection, Host.Index))
                            .AddControl(Of String)(TXT_ACCOUNT_NAME, TXT_ACCOUNT_NAME.CaptionText,
                                                   hostCollection.Count = 1 And Host.Index = 0, New AccountsNameChecker(hostCollection, Host.Index))
                        End With

                        Dim offset% = PropertyValueHost.LeftOffsetDefault
                        Dim h% = 0, c% = 0
                        Dim AddTpControl As Action(Of Control, Integer) = Sub(ByVal cnt As Control, ByVal _height As Integer)
                                                                              TP_SITE_PROPS.RowStyles.Add(New RowStyle(SizeType.Absolute, _height))
                                                                              TP_SITE_PROPS.RowCount += 1
                                                                              TP_SITE_PROPS.Controls.Add(cnt, 0, TP_SITE_PROPS.RowStyles.Count - 1)
                                                                              h += _height
                                                                              c += 1
                                                                          End Sub

                        If Host.Responser Is Nothing Then
                            h -= 28
                            TXT_COOKIES.Enabled = False
                            TXT_COOKIES.Visible = False
                            TP_MAIN.RowStyles(2).Height = 0
                        End If

                        If .PropList.Count > 0 Then
                            With TP_SITE_PROPS
                                With .RowStyles : .RemoveAt(.Count - 1) : End With
                                .RowCount -= 1
                            End With

                            Dim laAdded As Boolean = False
                            Dim loAdded As Boolean = False
                            Dim pArr() As Boolean
                            If .PropList.Exists(Function(p) If(p.Options?.IsAuth, False)) Then pArr = {True, False} Else pArr = {False}
                            .PropList.Sort()
                            For Each pAuth As Boolean In pArr
                                For Each prop As PropertyValueHost In .PropList
                                    If Not prop.Options Is Nothing Then
                                        With prop
                                            If .Options.IsAuth = pAuth Then

                                                If pArr.Length = 2 Then
                                                    Select Case pAuth
                                                        Case True
                                                            If Not laAdded Then AddTpControl(LBL_AUTH, 25) : laAdded = True
                                                        Case False
                                                            If Not loAdded Then AddTpControl(LBL_OTHER, 25) : loAdded = True
                                                    End Select
                                                End If

                                                .CreateControl(TT_MAIN)
                                                AddTpControl(.Control, .ControlHeight)
                                                If .LeftOffset > offset Then offset = .LeftOffset
                                                If Not .Options.AllowNull Or Not .ProviderFieldsChecker Is Nothing Then _
                                                   MyDefs.MyFieldsCheckerE.AddControl(.Control, .Options.ControlText, .Type,
                                                                                      .Options.AllowNull, .ProviderFieldsChecker)
                                            End If
                                        End With
                                    End If
                                Next
                            Next
                        End If

                        SpecialButton = .GetSettingsButtonInternal
                        If Not SpecialButton Is Nothing Then AddTpControl(SpecialButton, 28)
                        TP_SITE_PROPS.BaseControlsPadding = New Padding(offset, 0, 0, 0)
                        offset += PaddingE.GetOf({TP_SITE_PROPS}).Left
                        TXT_PATH.CaptionWidth = offset
                        TXT_PATH_SAVED_POSTS.CaptionWidth = offset
                        TXT_COOKIES.CaptionWidth = offset
                        TXT_ACCOUNT_NAME.CaptionWidth = offset
                        CH_DOWNLOAD_SITE_DATA.Padding = New PaddingE(CH_DOWNLOAD_SITE_DATA.Padding) With {.Left = offset}
                        CH_USE_SAVED_POSTS.Padding = New PaddingE(CH_USE_SAVED_POSTS.Padding) With {.Left = offset}
                        CH_GET_USER_MEDIA_ONLY.Padding = New PaddingE(CH_GET_USER_MEDIA_ONLY.Padding) With {.Left = offset}
                        If c > 0 Or h <> 0 Then
                            Dim ss As New Size(Size.Width, Size.Height + h + c)
                            Dim minScrh% = Screen.AllScreens.Min(Function(scr) scr.WorkingArea.Height)

                            If ss.Height >= minScrh - 20 Then
                                ss.Height = minScrh - 40
                                With TP_SITE_PROPS
                                    .AutoScroll = True
                                    Dim p As Padding = .Padding
                                    p.Right = 3
                                    .Padding = p
                                    .PerformLayout()
                                End With
                            End If

                            MinimumSize = ss
                            Size = ss
                            MaximumSize = ss
                        End If
                    End With

                    .MyFieldsChecker.EndLoaderOperations()
                    .EndLoaderOperations()
                End With
            Catch ex As Exception
                MyDefs.InvokeLoaderError(ex)
            End Try
        End Sub
        Private Sub SiteEditorForm_Disposed(sender As Object, e As EventArgs) Handles Me.Disposed
            If Host.PropList.Count > 0 Then Host.PropList.ForEach(Sub(p) p.DisposeControl())
            If Not SpecialButton Is Nothing Then SpecialButton.Dispose()
            LBL_AUTH.Dispose()
            LBL_OTHER.Dispose()
            Host.Source.EndEdit()
            If Not Cookies Is Nothing Then Cookies.Dispose()
        End Sub
        Private Sub MyDefs_ButtonOkClick(ByVal Sender As Object, ByVal e As KeyHandleEventArgs) Handles MyDefs.ButtonOkClick
            If MyDefs.MyFieldsChecker.AllParamsOK Then
                Dim i%, ii%
                With Host
                    Dim indxList As New List(Of Integer)
                    For i = 0 To .PropList.Count - 1
                        If .PropList(i).PropertiesChecking.ListExists And Not .PropList(i).PropertiesCheckingMethod Is Nothing Then indxList.Add(i)
                    Next
                    If indxList.Count > 0 Then
                        Dim pList As New List(Of PropertyData)
                        Dim n$()
                        For i = 0 To indxList.Count - 1
                            n = .PropList(indxList(i)).PropertiesChecking
                            For ii = 0 To .PropList.Count - 1
                                With .PropList(ii)
                                    If n.Contains(.Name) Then pList.Add(New PropertyData(.Name, .GetControlValue))
                                End With
                            Next
                            If pList.Count > 0 AndAlso Not CBool(.PropList(indxList(i)).PropertiesCheckingMethod.Invoke(.Source, {pList})) Then Exit Sub
                        Next
                    End If

                    Settings.BeginUpdate()

                    SiteDefaultsFunctions.SetPropByChecker(TP_SITE_PROPS, Host)
                    If TXT_PATH.IsEmptyString Then .Path = Nothing Else .Path = TXT_PATH.Text
                    .SavedPostsPath = TXT_PATH_SAVED_POSTS.Text
                    .AccountName = TXT_ACCOUNT_NAME.Text
                    .DownloadSiteData.Value = CH_DOWNLOAD_SITE_DATA.Checked
                    .DownloadSavedPosts.Value = CH_USE_SAVED_POSTS.Checked
                    .GetUserMediaOnly.Value = CH_GET_USER_MEDIA_ONLY.Checked
                    If CookiesChanged And Not Cookies Is Nothing And Not .Responser Is Nothing Then
                        With .Responser.Cookies
                            .Clear()
                            .AddRange(Cookies,, EDP.LogMessageValue)
                        End With
                    End If

                    If .PropList.Count > 0 Then .PropList.ForEach(Sub(p) If Not p.Options Is Nothing Then p.UpdateValueByControl())

                    .Source.Update()
                End With

                Settings.EndUpdate()

                MyDefs.CloseForm()
            End If
        End Sub
        Private _PathBefore As SFile = Nothing
        Private Sub TXT_PATH_ActionOnBeforeTextChanged(sender As Object, e As EventArgs) Handles TXT_PATH.ActionOnBeforeTextChanged
            If Not MyDefs.Initializing Then _PathBefore = TXT_PATH.Text.CSFileP
        End Sub
        Private _PathButtonClicked As Boolean = False
        Private Sub TXT_PATH_ActionOnButtonClick(ByVal Sender As ActionButton, ByVal e As ActionButtonEventArgs) Handles TXT_PATH.ActionOnButtonClick
            _PathButtonClicked = True
            ChangePath(Sender, Host.Path(False), TXT_PATH)
            _PathButtonClicked = False
        End Sub
        Private Sub TXT_PATH_ActionOnTextChanged(sender As Object, e As EventArgs) Handles TXT_PATH.ActionOnTextChanged
            If (_PathButtonClicked Or MyDefs.Initializing) And (HostCollection.Count > 1 Or Host.Index = -1) And TXT_PATH_SAVED_POSTS.IsEmptyString Then
                If Not TXT_PATH.Text.IsEmptyString Then
                    Dim f As SFile = TXT_PATH_SAVED_POSTS.Text.CSFileP
                    If f.IsEmptyString OrElse (Not _PathBefore.IsEmptyString AndAlso _PathBefore.PathNoSeparator.Contains(f.PathNoSeparator)) Then _
                       TXT_PATH_SAVED_POSTS.Text = SavedPostsChecker.GetNewSavedPostsName(TXT_PATH.Text, Host.Index, HostCollection.Count)
                End If
            End If
        End Sub
        Private Sub TXT_PATH_SAVED_POSTS_ActionOnButtonClick(ByVal Sender As ActionButton, ByVal e As ActionButtonEventArgs) Handles TXT_PATH_SAVED_POSTS.ActionOnButtonClick
            If e.DefaultButton = ADB.Refresh Then
                If Not TXT_PATH.IsEmptyString Then TXT_PATH_SAVED_POSTS.Text =
                   SavedPostsChecker.GetNewSavedPostsName(TXT_PATH.Text, Host.Index, HostCollection.Count)
            Else
                ChangePath(Sender, Host.SavedPostsPath(False), TXT_PATH_SAVED_POSTS)
            End If
        End Sub
        Private Sub ChangePath(ByVal Sender As ActionButton, ByVal PathValue As SFile, ByRef CNT As TextBoxExtended)
            If Sender.DefaultButton = ADB.Open Then
                Dim f As SFile = SFile.SelectPath(PathValue).IfNullOrEmpty(PathValue)
                If Not f.IsEmptyString Then CNT.Text = f
            End If
        End Sub
        Private Sub TXT_COOKIES_ActionOnButtonClick(ByVal Sender As ActionButton, ByVal e As EventArgs) Handles TXT_COOKIES.ActionOnButtonClick
            Select Case Sender.DefaultButton
                Case ADB.Edit
                    If Not Cookies Is Nothing Then
                        Using f As New CookieListForm With {.DesignXML = Settings.Design, .ShowGrid = False}
                            f.SetCollection(Cookies)
                            f.ShowDialog()
                            If f.DialogResult = DialogResult.OK Then
                                CookiesChanged = True
                                f.GetCollection(Cookies)
                                MyDefs.MyOkCancel.EnableOK = True
                            End If
                        End Using
                        SetCookieText()
                    End If
                Case ADB.Clear
                    If Not Cookies Is Nothing Then
                        CookiesChanged = True
                        Cookies.Clear()
                        MyDefs.MyOkCancel.EnableOK = True
                        SetCookieText()
                    End If
            End Select
        End Sub
        Private Sub SetCookieText()
            If Not Cookies Is Nothing Then TXT_COOKIES.Text = $"{Cookies.Count} cookies"
        End Sub
        Private Sub SpecialButton_Click(sender As Object, e As EventArgs) Handles SpecialButton.Click
            MyDefs.Detector()
        End Sub
    End Class
End Namespace