Option Strict On
Option Infer On
Imports VbPixelGameEngine

Public NotInheritable Class AtariPong
    Inherits PixelGameEngine

    Private Declare Unicode Function PostMessageW Lib "user32.dll" _
        (hhwnd As Integer, msg As UInteger, wparam As IntPtr, lparam As IntPtr) As Boolean

    Private Declare Unicode Function LoadKeyboardLayoutW Lib "user32.dll" _
        (pwszKLID As String, Flags As UInteger) As IntPtr

    Friend Shared Sub DisableIME()
        ' The following line defaults the input method to English mode.
        PostMessageW(&HFFFF, &H50, IntPtr.Zero, LoadKeyboardLayoutW("00000409", 1))
    End Sub

    Friend Shared Sub Main()
        With New AtariPong
            If .Construct(screenW:=400, screenH:=300, fullScreen:=True) Then .Start()
        End With
    End Sub

    Public Sub New()
        AppName = "Atari Pong Clone in VB.NET"
        DisableIME()
    End Sub

    Private Enum GameState As Byte
        TitleScreen = 0
        SinglePlayer = 1
        TwoPlayers = 2
        Paused = 3
        GameOver = 4
    End Enum

    Private Const WINNING_SCORE As Integer = 11
    Private currGameState As GameState = GameState.TitleScreen
    Private gameMode As GameState, gameWinner As String

    ' Game elements as follows.
    Private ReadOnly paddleSize As New Vi2d(10, 50)
    Private leftPaddleX As Integer, leftPaddleY As Single
    Private rightPaddleX As Integer, rightPaddleY As Single
    Private ballX As Single, ballY As Single
    Private ballSpeedX As Single, ballSpeedY As Single

    Private Shadows ReadOnly rnd As New Random
    Private leftScore As Integer, rightScore As Integer
    Private Const PADDLE_SPEED As Single = 200.0F
    Private Const BASE_BALL_SPEED As Single = 150.0F

    Private Sub ResetPaddles()
        leftPaddleX = 10
        rightPaddleX = ScreenWidth - 10 - paddleSize.x
        leftPaddleY = CSng(ScreenHeight / 2 - paddleSize.y / 2)
        rightPaddleY = CSng(ScreenHeight / 2 - paddleSize.y / 2)
    End Sub

    Protected Overrides Function OnUserCreate() As Boolean
        leftScore = 0
        rightScore = 0
        ResetPaddles()
        ResetBall()
        Return True
    End Function

    Private Sub ResetBall()
        ' Center the ball.
        ballX = CSng(ScreenWidth / 2 - 5)
        ballY = CSng(ScreenHeight / 2 - 5)

        ' Randomize initial direction.
        ballSpeedX = BASE_BALL_SPEED * If(rnd.Next(0, 2) = 0, -1, 1)
        ballSpeedY = BASE_BALL_SPEED * (rnd.Next(-100, 101) / 100.0F) * 0.6F
    End Sub

    Private Sub UpdateAI(dt As Single, errorMargin As Single, sensitivity As Single)
        Dim IsOutOfRange = Function(num As Single) num < 0 OrElse num > 1

        If IsOutOfRange(errorMargin) OrElse IsOutOfRange(sensitivity) Then
            Throw New ArgumentException("Error margin or sensitivity must be within [0, 1].")
        End If
        ' Only control right paddle in single-player mode.
        If currGameState <> GameState.SinglePlayer Then Exit Sub

        ' Predict ball position with some imperfection (only when the ball moves towards AI).
        Dim predictedY As Single = ballY
        If ballSpeedX > 0 Then
            Dim timeToReach As Single = (rightPaddleX - ballX) / ballSpeedX
            predictedY = ballY + ballSpeedY * timeToReach
            ' Add imperfection based on difficulty.
            predictedY += (rnd.NextSingle() - 0.5F) * errorMargin * paddleSize.y
        End If

        ' Target position (center of paddle should align with predicted ball position).
        Dim targetY As Single = predictedY - paddleSize.y / 2.0F
        ' Keep target within screen bounds.
        targetY = Math.Max(0, Math.Min(ScreenHeight - paddleSize.y, targetY))

        ' Move toward target with adequate sensitivity.
        If Math.Abs(rightPaddleY - targetY) > 1 Then
            Dim deltaY As Single = PADDLE_SPEED * dt * sensitivity
            rightPaddleY += If(rightPaddleY < targetY, deltaY, -deltaY)
        End If
    End Sub

    Protected Overrides Function OnUserUpdate(elapsedTime As Single) As Boolean
        Dim IsSinglePlayer = Function() currGameState = GameState.SinglePlayer
        Dim ReprTwoDigits = Function(i As Integer) i.ToString().PadLeft(2)
        Dim IsValidScoreDiff = Function() Math.Abs(leftScore - rightScore) >= 2

        ' Handle game state transitions.
        If GetKey(Key.SPACE).Pressed AndAlso currGameState = GameState.GameOver Then
            currGameState = gameMode
            Call OnUserCreate()
        End If

        If currGameState = GameState.TitleScreen Then
            If GetKey(Key.K1).Pressed Then
                gameMode = GameState.SinglePlayer
                currGameState = gameMode
            ElseIf GetKey(Key.K2).Pressed Then
                gameMode = GameState.TwoPlayers
                currGameState = gameMode
            End If
        End If

        If GetKey(Key.P).Pressed Then
            Select Case currGameState
                Case GameState.TwoPlayers, GameState.SinglePlayer
                    currGameState = GameState.Paused
                Case GameState.Paused
                    currGameState = gameMode
            End Select
        End If

        If GetKey(Key.ESCAPE).Pressed Then
            If {GameState.GameOver, GameState.Paused}.Contains(currGameState) Then
                currGameState = GameState.TitleScreen
                Call OnUserCreate()
            Else
                Return False
            End If
        End If

        ' Only update game logic when playing.
        If {GameState.TwoPlayers, GameState.SinglePlayer}.Contains(currGameState) Then
            ' Handle input for left paddle.
            If GetKey(Key.W).Held Then leftPaddleY -= PADDLE_SPEED * elapsedTime
            If GetKey(Key.S).Held Then leftPaddleY += PADDLE_SPEED * elapsedTime

            ' Handle input for right paddle in two-player mode.
            If currGameState = GameState.TwoPlayers Then
                If GetKey(Key.UP).Held Then rightPaddleY -= PADDLE_SPEED * elapsedTime
                If GetKey(Key.DOWN).Held Then rightPaddleY += PADDLE_SPEED * elapsedTime
            End If

            ' Update AI in single-player mode.
            If currGameState = GameState.SinglePlayer Then UpdateAI(elapsedTime, 0.2F, 0.3F)

            ' Keep paddles on screen.
            leftPaddleY = Math.Max(0, Math.Min(ScreenHeight - paddleSize.y, leftPaddleY))
            rightPaddleY = Math.Max(0, Math.Min(ScreenHeight - paddleSize.y, rightPaddleY))

            ' Update ball position.
            ballX += ballSpeedX * elapsedTime
            ballY += ballSpeedY * elapsedTime

            ' Top/bottom wall collision.
            If ballY <= 0 Then
                ballY = 0
                ballSpeedY = -ballSpeedY
            ElseIf ballY >= ScreenHeight - 10 Then
                ballY = ScreenHeight - 10
                ballSpeedY = -ballSpeedY
            End If

            ' Left paddle collision.
            If ballX <= leftPaddleX + paddleSize.x AndAlso
               ballX + 10 >= leftPaddleX AndAlso
               ballY + 10 >= leftPaddleY AndAlso
               ballY <= leftPaddleY + paddleSize.y AndAlso
               ballSpeedX < 0 Then

                ' Calculate hit position effect.
                Dim hitPos As Single = ballY + 5 - leftPaddleY
                Dim normalized As Single = (hitPos / paddleSize.y) - 0.5F

                ' Reverse and adjust ball velocity.
                ballSpeedX = -ballSpeedX * 1.1F
                ballSpeedY += normalized * 200

                ' Prevent sticking.
                ballX = leftPaddleX + paddleSize.x
            End If

            ' Right paddle collision.
            If ballX + 10 >= rightPaddleX AndAlso
               ballX <= rightPaddleX + paddleSize.x AndAlso
               ballY + 10 >= rightPaddleY AndAlso
               ballY <= rightPaddleY + paddleSize.y AndAlso
               ballSpeedX > 0 Then

                Dim hitPos As Single = ballY + 5 - rightPaddleY
                Dim normalized As Single = (hitPos / paddleSize.y) - 0.5F

                ballSpeedX = -ballSpeedX * 1.1F
                ballSpeedY += normalized * 200

                ballX = rightPaddleX - 10
            End If

            ' Cap vertical speed.
            If Math.Abs(ballSpeedY) > 300 Then ballSpeedY = 300 * Math.Sign(ballSpeedY)

            ' Scoring logic as follows.
            If ballX < -10 Then
                rightScore += 1
                If rightScore >= WINNING_SCORE AndAlso IsValidScoreDiff() Then
                    gameWinner = If(IsSinglePlayer(), "COMPUTER", "PLAYER 2")
                    currGameState = GameState.GameOver
                Else
                    ResetPaddles()
                    ResetBall()
                End If
            ElseIf ballX > ScreenWidth Then
                leftScore += 1
                If leftScore >= WINNING_SCORE AndAlso IsValidScoreDiff() Then
                    gameWinner = "PLAYER 1"
                    currGameState = GameState.GameOver
                Else
                    ResetPaddles()
                    ResetBall()
                End If
            End If
        End If

        Select Case currGameState
            Case GameState.TitleScreen
                Clear()
                ' Draw game title.
                DrawString(0, 10, "ATARI PONG CLONE", Presets.Yellow, 3)

                ' Draw game mode options.
                DrawString(0, 60, "SELECT MODE BY PRESSING:", Presets.White, 2)
                DrawString(0, 85, "KEY ""1"" FOR SINGLE PLAYER", Presets.White, 2)
                DrawString(0, 105, "KEY ""2"" FOR TWO PLAYERS", Presets.White, 2)

                ' Draw instructions.
                DrawString(30, 140, "- PADDLE MOVEMENT -", Presets.Apricot, 2)
                DrawString(0, 165, "P1AYER 1 WITH ""W"" AND ""S""", Presets.Apricot, 2)
                DrawString(0, 185, "PLAYER 2 WITH UP AND DOWN", Presets.Apricot, 2)


                DrawString(30, 220, "- WHILE PLAYING -", Presets.Beige, 2)
                DrawString(0, 245, """P"": PAUSE, ""ESC"": EXIT", Presets.Beige, 2)
            Case GameState.TwoPlayers, GameState.SinglePlayer
                Clear()
                ' Draw center line (dashed).
                For y As Integer = 0 To ScreenHeight Step 20
                    DrawLine(ScreenWidth \ 2, y, ScreenWidth \ 2, y + 10)
                Next y
                ' Draw game elements.
                FillRect(leftPaddleX, CInt(leftPaddleY), paddleSize.x, paddleSize.y)
                FillRect(rightPaddleX, CInt(rightPaddleY), paddleSize.x, paddleSize.y)
                FillRect(CInt(ballX), CInt(ballY), 10, 10)
                DrawString(ScreenWidth \ 2 - 60, 10, ReprTwoDigits(leftScore), Presets.White, 3)
                DrawString(ScreenWidth \ 2 + 10, 10, ReprTwoDigits(rightScore), Presets.White, 3)

                ' Draw game mode indicator.
                Dim gameModeText = If(IsSinglePlayer(), "SINGLE PLAYER", "TWO PLAYERS")
                DrawString(10, ScreenHeight - 30, $"MODE: {gameModeText}", Presets.Cyan)

            Case GameState.Paused
                Clear(Presets.DarkBlue)
                DrawString(50, ScreenHeight \ 2 - 50, "GAME PAUSED", Presets.Yellow, 3)
                DrawString(20, ScreenHeight \ 2 + 30, "PRESS ""P"" TO RESUME", Presets.White, 2)
                DrawString(20, ScreenHeight \ 2 + 60, "PRESS ""ESC"" FOR MENU", Presets.White, 2)
            Case GameState.GameOver
                Clear(If(gameWinner = "COMPUTER", Presets.DarkRed, Presets.DarkGreen))
                DrawString(20, ScreenHeight \ 3, gameWinner & " WINS!", Presets.Yellow, 3)
                DrawString(20, ScreenHeight \ 2 + 20, "PRESS SPACE TO RESTART", Presets.White, 2)
                DrawString(20, ScreenHeight \ 2 + 50, "PRESS ""ESC"" FOR MENU", Presets.White, 2)
        End Select

        Return True
    End Function
End Class