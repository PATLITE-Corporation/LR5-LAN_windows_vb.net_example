Imports System.Net
Imports System.Net.Sockets
Imports System.Runtime.InteropServices

Module Main
    Private sock As Socket = Nothing

    ' product category
    Public Const PNS_PRODUCT_ID As UShort = &H4142

    ' PNS command identifier
    Private Const PNS_RUN_CONTROL_COMMAND As Byte = &H53        ' operation control command
    Private Const PNS_CLEAR_COMMAND As Byte = &H43              ' clear command
    Private Const PNS_GET_DATA_COMMAND As Byte = &H47           ' get status command

    ' response data for PNS commands
    Private Const PNS_ACK As Byte = &H6      ' normal response
    Private Const PNS_NAK As Byte = &H15     ' abnormal response

    ' LED unit for motion control command

    Private Const PNS_RUN_CONTROL_LED_OFF As Byte = &H0                    　   ' light off
    Private Const PNS_RUN_CONTROL_LED_ON As Byte = &H1                      　  ' light on
    Private Const PNS_RUN_CONTROL_LED_BLINKING_SLOW As Byte = &H2 　    ' blinking(slow)
    Private Const PNS_RUN_CONTROL_LED_BLINKING_MEDIUM As Byte = &H3    ' blinking(medium)
    Private Const PNS_RUN_CONTROL_LED_BLINKING_HIGH As Byte = &H4  　　  ' blinking(high)
    Private Const PNS_RUN_CONTROL_LED_FLASHING_SINGLE As Byte = &H5    'flashing single
    Private Const PNS_RUN_CONTROL_LED_FLASHING_DOUBLE As Byte = &H6  'flashing double
    Private Const PNS_RUN_CONTROL_LED_FLASHING_TRIPLE As Byte = &H7    'flashing triple
    Private Const PNS_RUN_CONTROL_LED_NO_CHANGE As Byte = &H9             'no change

    ' buzzer for motion control command
    Private Const PNS_RUN_CONTROL_BUZZER_STOP As Byte = &H0               ' stop
    Private Const PNS_RUN_CONTROL_BUZZER_RING As Byte = &H1                ' ring
    Private Const PNS_RUN_CONTROL_BUZZER_NO_CHANGE As Byte = &H9    ' no change

    ' operation control data structure
    Public Class PNS_RUN_CONTROL_DATA
        ' LED Red pattern
        Public ledRedPattern As Byte = 0

        ' LED Amber pattern
        Public ledAmberPattern As Byte = 0

        ' LED Green pattern
        Public ledGreenPattern As Byte = 0

        ' LED Blue pattern
        Public ledBluePattern As Byte = 0

        ' LED White pattern
        Public ledWhitePattern As Byte = 0

        ' buzzer pattern 1 to 3
        Public buzzerMode As Byte = 0
    End Class

    ' status data of operation control
    Public Class PNS_STATUS_DATA
        ' LED Pattern 1 to 5
        Public ledPattern As Byte() = New Byte(5) {}

        ' buzzer mode
        Public buzzer As Byte = 0

    End Class


    ' <summary>
    ' Main Function
    ' </summary>
    Sub Main()
        Dim ret As Integer

        ' Connect to LR5-LAN
        ret = SocketOpen("192.168.10.1", 10000)
        If ret = -1 Then
            Return
        End If

        ' Get the command identifier specified by the command line argument
        Dim commandId As String = ""
        Dim cmds As String() = System.Environment.GetCommandLineArgs()
        If cmds.Length > 1 Then
            commandId = cmds(1)
        End If

        Select Case commandId
            Case "S"
                ' operation control command
                If cmds.Length >= 8 Then
                    Dim runControlData As PNS_RUN_CONTROL_DATA = New PNS_RUN_CONTROL_DATA With {
                        .ledRedPattern = cmds(2),
                        .ledAmberPattern = cmds(3),
                        .ledGreenPattern = cmds(4),
                        .ledBluePattern = cmds(5),
                        .ledWhitePattern = cmds(6),
                        .buzzerMode = cmds(7)
                    }
                    PNS_RunControlCommand(runControlData)
                End If

            Case "C"
                ' clear command
                PNS_ClearCommand()

            Case "G"
                ' get status command
                Dim statusData As PNS_STATUS_DATA = New PNS_STATUS_DATA
                ret = PNS_GetDataCommand(statusData)
                If ret = 0 Then
                    ' Display acquired data
                    Console.WriteLine("Response data for status acquisition command")
                    ' LED Red pattern
                    Console.WriteLine("LED Red pattern : " + statusData.ledPattern(0).ToString())
                    ' LED Amber pattern
                    Console.WriteLine("LED Amber pattern : " + statusData.ledPattern(1).ToString())
                    ' LED Green pattern
                    Console.WriteLine("LED Green pattern : " + statusData.ledPattern(2).ToString())
                    ' LED Blue pattern
                    Console.WriteLine("LED Blue pattern : " + statusData.ledPattern(3).ToString())
                    ' LED White pattern
                    Console.WriteLine("LED White pattern : " + statusData.ledPattern(4).ToString())
                    ' buzzer mode
                    Console.WriteLine("buzzer mode : " + statusData.buzzer.ToString())

                End If

        End Select

        ' Close the socket
        SocketClose()
    End Sub

    ''' <summary>
    ''' Connect to LR5-LAN
    ''' </summary>
    ''' <param name="ip">IP address</param>
    ''' <param name="port">port number</param>
    ''' <returns>success: 0, failure: non-zero</returns>
    Public Function SocketOpen(ByVal ip As String, ByVal port As Integer) As Integer
        Try
            ' Set the IP address and port
            Dim ipAddress As IPAddress = IPAddress.Parse(ip)
            Dim remoteEP As IPEndPoint = New IPEndPoint(ipAddress, port)
            sock = New Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp)

            ' Create a socket
            If sock Is Nothing Then
                Console.WriteLine("failed to create socket")
                Return -1
            End If

            ' Connect to LR5-LAN
            sock.Connect(remoteEP)
        Catch ex As Exception
            Console.WriteLine(ex.Message)
            SocketClose()
            Return -1
        End Try

        Return 0
    End Function

    ''' <summary>
    ''' Close the socket.
    ''' </summary>
    Public Sub SocketClose()
        If sock IsNot Nothing Then
            ' Close the socket.
            sock.Shutdown(SocketShutdown.Both)
            sock.Close()
        End If
    End Sub

    ''' <summary>
    ''' Send command
    ''' </summary>
    ''' <param name="sendData">send data</param>
    ''' <param name="recvData">received data</param>
    ''' <returns>success: 0, failure: non-zero</returns>
    Public Function SendCommand(ByVal sendData As Byte(), ByRef recvData As Byte()) As Integer
        Dim ret As Integer
        recvData = Nothing

        Try

            If sock Is Nothing Then
                Console.WriteLine("socket is not")
                Return -1
            End If

            ' Send
            ret = sock.Send(sendData)
            If ret < 0 Then
                Console.WriteLine("failed to send")
                Return -1
            End If

            ' Receive response data
            Dim bytes As Byte() = New Byte(1023) {}
            Dim recvSize As Integer = sock.Receive(bytes)
            If recvSize < 0 Then
                Console.WriteLine("failed to recv")
                Return -1
            End If

            recvData = New Byte(recvSize - 1) {}
            Array.Copy(bytes, recvData, recvSize)
        Catch ex As Exception
            Console.WriteLine(ex.Message)
            Return -1
        End Try

        Return 0
    End Function


    ''' <summary>
    ''' Send operation control command for PNS command
    ''' Each color of the LED unit and the buzzer can be controlled by the pattern specified in the data area
    ''' Operates with the color and buzzer set in the signal light mode
    ''' </summary>
    ''' <param name="runControlData">
    ''' Red/amber/green/blue/white LED unit operation patterns, buzzer mode
    ''' Pattern of LED unit (off: 0, on: 1, blinking(slow): 2, blinking(medium): 3, blinking(high): 4, flashing single: 5, flashing double: 6, flashing triple: 7, no change: 9)
    ''' Pattern of buzzer (stop: 0, ring: 1, no change: 9)
    ''' </param>
    ''' <returns>success: 0, failure: non-zero</returns>
    Public Function PNS_RunControlCommand(ByVal runControlData As PNS_RUN_CONTROL_DATA) As Integer
        Dim ret As Integer

        Try
            Dim sendData As Byte() = {}

            ' Product Category (AB)
            sendData = sendData.Concat(BitConverter.GetBytes(PNS_PRODUCT_ID).Reverse()).ToArray()

            ' Command identifier (S)
            sendData = sendData.Concat(New Byte() {PNS_RUN_CONTROL_COMMAND}).ToArray()

            ' Empty (0)
            sendData = sendData.Concat(New Byte() {0}).ToArray()

            ' data size, data area
            Dim data As Byte() = {
                runControlData.ledRedPattern,     ' LED Red pattern
                runControlData.ledAmberPattern,     ' LED Amber pattern
                runControlData.ledGreenPattern,     ' LED Green pattern
                runControlData.ledBluePattern,     ' LED Blue pattern
                runControlData.ledWhitePattern,     ' LED White pattern
                runControlData.buzzerMode      ' Buzzer pattern 1 to 3
            }
            sendData = sendData.Concat(BitConverter.GetBytes(CUShort(data.Length)).Reverse()).ToArray()
            sendData = sendData.Concat(data).ToArray()

            ' Send PNS command
            Dim recvData As Byte() = {}
            ret = SendCommand(sendData, recvData)
            If ret <> 0 Then
                Console.WriteLine("failed to send data")
                Return -1
            End If

            ' check the response data
            If recvData(0) = PNS_NAK Then
                ' receive abnormal response
                Console.WriteLine("negative acknowledge")
                Return -1
            End If

        Catch ex As Exception
            Console.WriteLine(ex.Message)
            Return -1
        End Try

        Return 0
    End Function

    ''' <summary>
    ''' Send clear command for PNS command
    ''' Turn off the LED unit and stop the buzzer
    ''' </summary>
    ''' <returns>success: 0, failure: non-zero</returns>
    Public Function PNS_ClearCommand() As Integer
        Dim ret As Integer

        Try
            Dim sendData As Byte() = {}

            ' Product Category (AB)
            sendData = sendData.Concat(BitConverter.GetBytes(PNS_PRODUCT_ID).Reverse()).ToArray()

            ' Command identifier (C)
            sendData = sendData.Concat(New Byte() {PNS_CLEAR_COMMAND}).ToArray()

            ' Empty (0)
            sendData = sendData.Concat(New Byte() {0}).ToArray()

            ' Data size
            sendData = sendData.Concat(BitConverter.GetBytes(CUShort(0))).ToArray()

            ' Send PNS command
            Dim recvData As Byte() = {}
            ret = SendCommand(sendData, recvData)
            If ret <> 0 Then
                Console.WriteLine("failed to send data")
                Return -1
            End If

            ' check the response data
            If recvData(0) = PNS_NAK Then
                ' receive abnormal response
                Console.WriteLine("negative acknowledge")
                Return -1
            End If

        Catch ex As Exception
            Console.WriteLine(ex.Message)
            Return -1
        End Try

        Return 0
    End Function

    ''' <summary>
    ''' Send status acquisition command for PNS command
    ''' LED unit and buzzer status can be acquired
    ''' </summary>
    ''' <param name="statusData">Received data of status acquisition command (status of LED unit and buzzer)</param>
    ''' <returns>Success: 0, failure: non-zero</returns>
    Public Function PNS_GetDataCommand(ByRef statusData As PNS_STATUS_DATA) As Integer
        Dim ret As Integer
        statusData = New PNS_STATUS_DATA()

        Try
            Dim sendData As Byte() = {}

            ' Product Category (AB)
            sendData = sendData.Concat(BitConverter.GetBytes(PNS_PRODUCT_ID).Reverse()).ToArray()

            ' Command identifier (G)
            sendData = sendData.Concat(New Byte() {PNS_GET_DATA_COMMAND}).ToArray()

            ' Empty (0)
            sendData = sendData.Concat(New Byte() {0}).ToArray()

            ' Data size
            sendData = sendData.Concat(BitConverter.GetBytes(CShort(0)).Reverse()).ToArray()

            ' Send PNS command
            Dim recvData As Byte() = {}
            ret = SendCommand(sendData, recvData)
            If ret <> 0 Then
                Console.WriteLine("failed to send data")
                Return -1
            End If

            ' check the response data
            If recvData(0) = PNS_NAK Then
                ' receive abnormal response
                Console.WriteLine("negative acknowledge")
                Return -1
            End If

            ' LED Pattern 1 to 5
            statusData.ledPattern = New Byte(5) {}
            Array.Copy(recvData, statusData.ledPattern, statusData.ledPattern.Length)

            ' Buzzer Mode
            statusData.buzzer = recvData(5)

        Catch ex As Exception
            Console.WriteLine(ex.Message)
            Return -1
        End Try

        Return 0
    End Function

End Module
