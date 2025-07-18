﻿' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Imports System.Threading
Imports SCrawler.Plugin.Hosts
Imports PersonalUtilities.Forms.Toolbars
Imports PDownload = SCrawler.Plugin.ISiteSettings.Download
Namespace API.Base
    Friend NotInheritable Class ProfileSaved
        Private ReadOnly Property HOST As SettingsHostCollection
        Private ReadOnly Property Progress As MyProgress
        Private _Unavailable As Integer, _NotReady As Integer, _ErrorCount As Integer
        Private _TotalImages As Integer, _TotalVideos As Integer
        Friend Sub New(ByRef h As SettingsHostCollection, ByRef Bar As MyProgress)
            HOST = h
            Progress = Bar
        End Sub
        Friend Overloads Sub Download(ByVal Token As CancellationToken, ByVal Multiple As Boolean)
            Dim n% = 0
            Dim c% = HOST.Sum(Function(h) IIf(h.DownloadSavedPosts, 1, 0))
            _Unavailable = 0
            _NotReady = 0
            _ErrorCount = 0
            _TotalImages = 0
            _TotalVideos = 0
            If c > 0 Then
                For i% = 0 To HOST.Count - 1
                    If HOST(i).DownloadSavedPosts Then n += 1 : Download(HOST(i), n, c, Token, Multiple)
                Next
                If c > 1 Then
                    Dim s% = {_Unavailable, _NotReady, _ErrorCount}.Sum
                    Progress.InformationTemporary = $"{HOST.Name} ({c - s}/{c}) Images: {_TotalImages}; Videos: {_TotalVideos}"
                End If
            End If
        End Sub
        Private Overloads Sub Download(ByVal Host As SettingsHost, ByVal Number As Integer, ByVal Count As Integer,
                                       ByVal Token As CancellationToken, ByVal Multiple As Boolean)
            Dim aStr$ = String.Empty
            If Count > 1 Then aStr = $" ({Number}/{Count})"
            Try
                If Host.Source.ReadyToDownload(PDownload.SavedPosts) Then
                    If Host.Available(PDownload.SavedPosts, Multiple Or Count > 1) Then
                        Host.DownloadStarted(PDownload.SavedPosts)
                        If Count > 1 Then Progress.Information = $"{Host.Name} - {Host.AccountName.IfNullOrEmpty(SettingsHost.NameAccountNameDefault)}"
                        Using user As IUserData = Host.GetInstance(PDownload.SavedPosts, Nothing, False, False)
                            If Not user Is Nothing Then
                                With DirectCast(user, UserDataBase)
                                    .HostStatic = True
                                    .IsSavedPosts = True
                                    .LoadUserInformation()
                                    .Progress = Progress
                                    If Not .FileExists Then .UpdateUserInformation()
                                End With
                                Host.BeforeStartDownload(user, PDownload.SavedPosts)
                                user.DownloadData(Token)
                                _TotalImages += user.DownloadedPictures(False)
                                _TotalVideos += user.DownloadedVideos(False)
                                Progress.InformationTemporary = $"{Host.Name}{aStr} Images: {user.DownloadedPictures(False)}; Videos: {user.DownloadedVideos(False)}"
                                Host.AfterDownload(user, PDownload.SavedPosts)
                            End If
                        End Using
                    Else
                        _Unavailable += 1
                        Progress.InformationTemporary = $"Host [{Host.Name}{aStr}] is unavailable"
                    End If
                Else
                    _NotReady += 1
                    Progress.InformationTemporary = $"Host [{Host.Name}{aStr}] is not ready"
                End If
            Catch ex As Exception
                _ErrorCount += 1
                Progress.InformationTemporary = $"{Host.Name}{aStr} downloading error"
                ErrorsDescriber.Execute(EDP.SendToLog, ex, $"[API.Base.ProfileSaved.Download({Host.Key}{aStr})]")
            Finally
                Host.DownloadDone(PDownload.SavedPosts)
                MainFrameObj.UpdateLogButton()
            End Try
        End Sub
    End Class
End Namespace