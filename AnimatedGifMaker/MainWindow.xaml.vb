Imports System.IO
Imports Microsoft.Win32

Class MainWindow

    Dim imageFiles As New List(Of String)

    Private Sub MainWindow_Loaded(sender As Object, e As EventArgs) Handles Me.Loaded
        Validate()
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
            CreateAnimatedGif(saveFileDialog.FileName + "_1.gif")
            CreateAnimatedGif2(saveFileDialog.FileName + "_2.gif")
            CreateAnimatedGif3(saveFileDialog.FileName + "_3.gif", 3)
            'CreateAnimatedGif4(saveFileDialog.FileName + "_4.gif", 3)
            CreateAnimatedGif5(saveFileDialog.FileName + "_5.gif", 5)
            CreateAnimatedGif6(saveFileDialog.FileName + "_6.gif", 5)
        End If
    End Sub

    Private Sub Validate()
        button_Output.IsEnabled = imageFiles.Count > 0
    End Sub

    ''' <summary>
    ''' https://dobon.net/vb/dotnet/graphics/createanimatedgif.html
    ''' 上記URLに記載されている実装。これだと、コマ送り速度も指定できないし、ループ再生もできない。
    ''' </summary>
    ''' <param name="outputPath"></param>
    Private Sub CreateAnimatedGif(outputPath As String)
        Dim encoder As New GifBitmapEncoder()

        For Each f As String In imageFiles
            Dim bmpFrame As BitmapFrame = BitmapFrame.Create(New Uri(f, UriKind.RelativeOrAbsolute))
            encoder.Frames.Add(bmpFrame)
        Next

        Using outputFileStrm As New FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None)
            encoder.Save(outputFileStrm)
        End Using
    End Sub

    ''' <summary>
    ''' https://stackoverflow.com/questions/18719302/net-creating-a-looping-gif-using-gifbitmapencoder
    ''' 上記URLに記載されている実装。
    ''' Application Extensionブロックが追加されたことで、ループ再生になる。
    ''' この実装ではループ回数は固定（無制限）。
    ''' </summary>
    ''' <param name="outputPath"></param>
    Private Sub CreateAnimatedGif2(outputPath As String)
        Dim encoder As New GifBitmapEncoder()

        For Each f As String In imageFiles
            Dim bmpFrame As BitmapFrame = BitmapFrame.Create(New Uri(f, UriKind.RelativeOrAbsolute))
            encoder.Frames.Add(bmpFrame)
        Next

        Using ms As New MemoryStream()
            encoder.Save(ms)
            Dim fileBytes = ms.ToArray()
            Dim applicationExtension As Byte() = {33, 255, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 0, 0, 0}
            Dim newBytes As New List(Of Byte)
            newBytes.AddRange(fileBytes.Take(13))
            newBytes.AddRange(applicationExtension)
            newBytes.AddRange(fileBytes.Skip(13))
            File.WriteAllBytes(outputPath, newBytes.ToArray())
        End Using
    End Sub

    ''' <summary>
    ''' CreateAnimatedGif2のループ回数を指定できるバージョン
    ''' </summary>
    ''' <param name="outputPath"></param>
    ''' <param name="loopCount"></param>
    Private Sub CreateAnimatedGif3(outputPath As String, loopCount As UInt16)
        Dim encoder As New GifBitmapEncoder()

        For Each f As String In imageFiles
            Dim bmpFrame As BitmapFrame = BitmapFrame.Create(New Uri(f, UriKind.RelativeOrAbsolute))
            encoder.Frames.Add(bmpFrame)
        Next

        Using ms As New MemoryStream()
            encoder.Save(ms)
            Dim fileBytes = ms.ToArray()
            Dim applicationExtension As Byte() = MakeApplicationExtension(loopCount)

            Dim newBytes As New List(Of Byte)
            newBytes.AddRange(fileBytes.Take(13))
            newBytes.AddRange(applicationExtension)
            newBytes.AddRange(fileBytes.Skip(13))
            File.WriteAllBytes(outputPath, newBytes.ToArray())
        End Using
    End Sub

    ''' <summary>
    ''' これまでの経験を踏まえて、Image Blockを計算してみた版。なんかうまくいかないことがあるので間違っているっぽい
    ''' </summary>
    ''' <param name="outputPath"></param>
    ''' <param name="loopCount"></param>
    Private Sub CreateAnimatedGif4(outputPath As String, loopCount As UInt16)
        Dim encoder As New GifBitmapEncoder()

        For Each f As String In imageFiles
            Dim bmpFrame As BitmapFrame = BitmapFrame.Create(New Uri(f, UriKind.RelativeOrAbsolute))
            encoder.Frames.Add(bmpFrame)
        Next

        Using ms As New MemoryStream()
            encoder.Save(ms)
            Dim fileBytes = ms.ToArray()
            Dim header = fileBytes.Take(13)
            Dim applicationExtension As Byte() = MakeApplicationExtension(loopCount)
            ' おそらくだが、この方法でEncodeしたGIFにはGlobal Color Tableはない
            ' Debug.WriteLine(header(10))

            Dim newBytes As New List(Of Byte)
            newBytes.AddRange(header)
            newBytes.AddRange(applicationExtension)

            Dim imageBlockBytes = fileBytes.Skip(13).ToList()
            Dim imageBlockSizeList = GetImageBlockSize(imageBlockBytes)
            For Each imageBlockSize As Int32 In imageBlockSizeList
                'newBytes.AddRange(GetGraphicControlExtension(100))

                newBytes.AddRange(imageBlockBytes.Take(imageBlockSize))
                imageBlockBytes = imageBlockBytes.Skip(imageBlockSize).ToList()
            Next

            File.WriteAllBytes(outputPath, newBytes.ToArray())
        End Using
    End Sub

    ''' <summary>
    ''' これまでの経験を踏まえて2
    ''' </summary>
    ''' <param name="outputPath"></param>
    ''' <param name="loopCount"></param>
    Private Sub CreateAnimatedGif5(outputPath As String, loopCount As UInt16)

        Using fs As New FileStream(outputPath, FileMode.Create), writer As New BinaryWriter(fs)
            Dim count = 0
            For Each f As String In imageFiles
                Dim encoder As New GifBitmapEncoder()
                Dim bmpFrame As BitmapFrame = BitmapFrame.Create(New Uri(f, UriKind.RelativeOrAbsolute))
                encoder.Frames.Add(bmpFrame)

                Using ms As New MemoryStream()
                    encoder.Save(ms)
                    Dim fileBytes = ms.ToArray()

                    If count = 0 Then
                        Dim header = fileBytes.Take(13)
                        Dim applicationExtension As Byte() = MakeApplicationExtension(loopCount)
                        ' おそらくだが、この方法でEncodeしたGIFにはGlobal Color Tableはない
                        ' Debug.WriteLine(header(10))

                        writer.Write(header.ToArray())
                        writer.Write(applicationExtension)
                    End If

                    writer.Write(fileBytes.Skip(13).Take(fileBytes.Count - 13 - 1).ToArray())
                    count += 1
                End Using
            Next

            writer.Write(&H3B) ' Trailer
        End Using
    End Sub

    ''' <summary>
    ''' これまでの経験を踏まえて3
    ''' </summary>
    ''' <param name="outputPath"></param>
    ''' <param name="loopCount"></param>
    Private Sub CreateAnimatedGif6(outputPath As String, loopCount As UInt16)

        Using fs As New FileStream(outputPath, FileMode.Create), writer As New BinaryWriter(fs)
            Dim encoderAll As New GifBitmapEncoder()
            For Each f As String In imageFiles
                Dim bmpFrame As BitmapFrame = BitmapFrame.Create(New Uri(f, UriKind.RelativeOrAbsolute))
                encoderAll.Frames.Add(bmpFrame)
            Next
            Using ms As New MemoryStream()
                encoderAll.Save(ms)
                Dim header = ms.ToArray().Take(13)
                Dim applicationExtension As Byte() = MakeApplicationExtension(loopCount)
                ' おそらくだが、この方法でEncodeしたGIFにはGlobal Color Tableはない
                Debug.WriteLine(header(10))

                writer.Write(header.ToArray())
                writer.Write(GetGraphicControlExtension(100))
                writer.Write(applicationExtension)
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

                    writer.Write(GetGraphicControlExtension(100))
                    writer.Write(fileBytes.Skip(13).Take(fileBytes.Count - 13 - 1).ToArray())
                End Using
            Next

            writer.Write(&H3B) ' Trailer
        End Using
    End Sub

    Private Function MakeApplicationExtension(loopCount As UInt16)
        Dim applicationExtension As Byte() = {33, 255, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 0, 0, 0}
        Dim loopCountBytes = BitConverter.GetBytes(loopCount)
        applicationExtension(16) = loopCountBytes(0)
        applicationExtension(17) = loopCountBytes(1)
        Return applicationExtension
    End Function

    Private Function GetImageBlockSize(ByVal imageDataBytes As List(Of Byte))
        Dim blocks As New List(Of Int32)

        Dim cursor = 0
        While cursor < imageDataBytes.Count
            Dim sizeOfLocalColorTable = GetSizeOfLocalColorTable(imageDataBytes.Skip(cursor))
            Dim sizeOfHeader = 10 + sizeOfLocalColorTable + 1

            Dim sizeOfData = 0
            Dim blockSize = imageDataBytes(cursor + sizeOfData) ' データ部分の大きさ
            While blockSize <> 0
                sizeOfData += 1 + blockSize ' ブロックサイズ1byte ＋ 実データ
                blockSize = imageDataBytes(cursor + sizeOfData) ' 次のブロックへ
            End While

            Dim sizeOfOneBlock = sizeOfHeader + sizeOfData
            blocks.Add(sizeOfOneBlock)
            cursor += sizeOfOneBlock
        End While

        Return blocks
    End Function

    ''' <summary>
    ''' Image Dataブロックの10バイト目から、Local Color Table（固有パレットデータ）の大きさを取得する。
    ''' </summary>
    ''' <param name="imageDataBytes">Image Dataブロックの先頭からのバイト配列</param>
    ''' <returns></returns>
    Private Function GetSizeOfLocalColorTable(ByVal imageDataBytes As IEnumerable(Of Byte))
        Dim flags = imageDataBytes(9)
        Debug.Write("flags = ")
        Debug.WriteLine(flags)
        If flags And &B10000000 Then
            ' Local Color Table Flag = 1
            Dim sizeOfLocalColorTableBit = (flags And &B111) + 1
            Return Math.Pow(2, sizeOfLocalColorTableBit)
        Else
            Return 0
        End If
    End Function

    ''' <summary>
    ''' Graphic Control Extension Blockを作る。
    ''' </summary>
    ''' <param name="delayTime"></param>
    ''' <returns></returns>
    Private Function GetGraphicControlExtension(ByVal delayTime As UInt16) As Byte()
        Dim graphicControlExtension As Byte() = {&H21, &HF9, &H4, &H0, 0, 0, &H0, &H0}
        Dim delayTimeBytes As Byte() = BitConverter.GetBytes(delayTime)
        graphicControlExtension(4) = delayTimeBytes(0)
        graphicControlExtension(5) = delayTimeBytes(1)
        Return graphicControlExtension
    End Function
End Class
