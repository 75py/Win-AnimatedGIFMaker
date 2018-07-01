Imports System.IO
Imports Microsoft.Win32
Imports System.Runtime.InteropServices

Class MainWindow

    Dim imageFiles As New List(Of String)

    Dim startupPath As String

    Private Sub MainWindow_Loaded(sender As Object, e As EventArgs) Handles Me.Loaded
        Dim exePath = Environment.GetCommandLineArgs()(0)
        Dim exeFullPath = Path.GetFullPath(exePath)
        startupPath = Path.GetDirectoryName(exeFullPath)

        Dim files As String() = System.Environment.GetCommandLineArgs()
        If files.Count > 1 Then
            For i = 1 To files.Count - 1
                Dim f = files(i)
                If File.Exists(f) Then
                    imageFiles.Add(f)
                End If
            Next
        End If
        Validate()

        LoadSettings()
    End Sub

    Private Sub MainWindow_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        SaveSettings()
    End Sub


    Private Sub button_SelectFiles_Click(sender As Object, e As RoutedEventArgs) Handles button_SelectFiles.Click
        Dim openFileDialog As New OpenFileDialog()
        openFileDialog.Filter = "Image Files|*.bmp;*.png;*.jpg;*.jpeg|All Files (*.*)|*.*"
        openFileDialog.Multiselect = True
        If openFileDialog.ShowDialog() Then
            imageFiles.Clear()
            imageFiles.AddRange(openFileDialog.FileNames)
        End If
        Validate()
    End Sub

    Private Sub button_Output_Click(sender As Object, e As RoutedEventArgs) Handles button_Output.Click
        Dim saveFileDialog As New SaveFileDialog()
        saveFileDialog.Filter = "Gif Files(*.gif)|*.gif"
        saveFileDialog.DefaultExt = "gif"
        If saveFileDialog.ShowDialog() Then
            CreateAnimatedGif(saveFileDialog.FileName, UInt16.Parse(textBox_delay.Text))
        End If
    End Sub

    Private Sub Validate()
        button_Output.IsEnabled = imageFiles.Count > 0
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="outputPath"></param>
    ''' <param name="delay"></param>
    Private Sub CreateAnimatedGif(outputPath As String, delay As UInt16)
        Using fs As New FileStream(outputPath, FileMode.Create), writer As New BinaryWriter(fs)
            Dim encoderAll As New GifBitmapEncoder()
            For Each f As String In imageFiles
                Dim bmpFrame As BitmapFrame = BitmapFrame.Create(New Uri(f, UriKind.RelativeOrAbsolute))
                encoderAll.Frames.Add(bmpFrame)
            Next
            Using ms As New MemoryStream()
                encoderAll.Save(ms)

                Dim header = ms.ToArray().Take(13)
                ' おそらくだが、この方法でEncodeしたGIFにはGlobal Color Tableはない
                Debug.WriteLine(header(10))
                writer.Write(header.ToArray())

                ' リピートありならApplication Extension Blockを挿入する
                If radioButton_RepeatOnce.IsChecked Then
                    writer.Write(MakeApplicationExtension(1))
                ElseIf radioButton_Repeat.IsChecked Then
                    writer.Write(MakeApplicationExtension(0))
                End If
            End Using

            For Each f As String In imageFiles
                Dim encoder As New GifBitmapEncoder()
                Dim bmpFrame As BitmapFrame = BitmapFrame.Create(New Uri(f, UriKind.RelativeOrAbsolute))
                encoder.Frames.Add(bmpFrame)

                Using ms As New MemoryStream()
                    encoder.Save(ms)
                    Dim fileBytes = ms.ToArray()

                    ' おそらくだが、この方法でEncodeしたGIFにはGlobal Color Tableはない
                    Dim header = ms.ToArray().Take(13)
                    Debug.WriteLine(header(10))

                    writer.Write(MakeGraphicControlExtension(delay))
                    writer.Write(fileBytes.Skip(13).Take(fileBytes.Count - 13 - 1).ToArray())
                End Using
            Next

            writer.Write(&H3B) ' Trailer
        End Using
    End Sub

    ''' <summary>
    ''' Application Extension Blockを作る。
    ''' </summary>
    ''' <param name="loopCount">ループ回数。0なら無制限</param>
    ''' <returns></returns>
    Private Function MakeApplicationExtension(loopCount As UInt16)
        Dim applicationExtension As Byte() = {33, 255, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 0, 0, 0}
        Dim loopCountBytes = BitConverter.GetBytes(loopCount)
        applicationExtension(16) = loopCountBytes(0)
        applicationExtension(17) = loopCountBytes(1)
        Return applicationExtension
    End Function

    ''' <summary>
    ''' Graphic Control Extension Blockを作る。
    ''' </summary>
    ''' <param name="delayTime">遅延時間(ms)</param>
    ''' <returns></returns>
    Private Function MakeGraphicControlExtension(ByVal delayTime As UInt16) As Byte()
        Dim graphicControlExtension As Byte() = {&H21, &HF9, &H4, &H0, 0, 0, &H0, &H0}
        ' 遅延時間の単位は1/100sec
        Dim d As UInt16 = delayTime / 10
        Dim delayTimeBytes As Byte() = BitConverter.GetBytes(d)
        graphicControlExtension(4) = delayTimeBytes(0)
        graphicControlExtension(5) = delayTimeBytes(1)
        Debug.WriteLine(delayTime)
        Debug.WriteLine(delayTimeBytes(0))
        Debug.WriteLine(delayTimeBytes(1))
        Return graphicControlExtension
    End Function

    <DllImport("KERNEL32.DLL")>
    Public Shared Function WritePrivateProfileString(
        ByVal lpAppName As String,
        ByVal lpKeyName As String,
        ByVal lpString As String,
        ByVal lpFileName As String) As Integer
    End Function

    <DllImport("KERNEL32.DLL", CharSet:=CharSet.Auto)>
    Public Shared Function GetPrivateProfileString(
        ByVal lpAppName As String,
        ByVal lpKeyName As String, ByVal lpDefault As String,
        ByVal lpReturnedString As System.Text.StringBuilder, ByVal nSize As Integer,
        ByVal lpFileName As String) As Integer
    End Function

    <DllImport("KERNEL32.DLL", CharSet:=CharSet.Auto)>
    Public Shared Function GetPrivateProfileInt(
        ByVal lpAppName As String,
        ByVal lpKeyName As String, ByVal nDefault As Integer,
        ByVal lpFileName As String) As Integer
    End Function

    Const INI_FILENAME = "\settings.ini"
    Const INI_SECTION = "FOO"
    Const INI_KEY_REPEAT = "REPEAT"
    Const INI_KEY_DELAY = "DELAY"

    Private Sub LoadSettings()
        Dim repeat = GetPrivateProfileInt(INI_SECTION, INI_KEY_REPEAT, 0, startupPath & INI_FILENAME)
        Select Case repeat
            Case 0
                radioButton_NoRepeat.IsChecked = True
            Case 1
                radioButton_RepeatOnce.IsChecked = True
            Case Else
                radioButton_Repeat.IsChecked = True
        End Select

        Dim delayStr As New System.Text.StringBuilder(capacity:=10)
        GetPrivateProfileString(INI_SECTION, INI_KEY_DELAY, "1000", delayStr, delayStr.Capacity, startupPath & INI_FILENAME)
        textBox_delay.Text = delayStr.ToString()
    End Sub

    Private Sub SaveSettings()
        Dim repeat As String
        If radioButton_NoRepeat.IsChecked Then
            repeat = "0"
        ElseIf radioButton_RepeatOnce.IsChecked Then
            repeat = "1"
        Else
            repeat = "2"
        End If
        WritePrivateProfileString(INI_SECTION, INI_KEY_REPEAT, repeat, startupPath & INI_FILENAME)

        WritePrivateProfileString(INI_SECTION, INI_KEY_DELAY, textBox_delay.Text, startupPath & INI_FILENAME)
    End Sub


End Class
