import pygame
from random import choice, uniform, random
from enum import Enum, unique

pygame.init()
SCREEN_WIDTH, SCREEN_HEIGHT = 800, 600
screen = pygame.display.set_mode((SCREEN_WIDTH, SCREEN_HEIGHT))
pygame.display.set_caption("Atari Pong Clone")

WHITE = (255, 255, 255)
WHEAT = (245, 222, 179)
YELLOW = (255, 255, 0)
BLUE = (0, 0, 255)
DARK_RED = (255, 0, 0)
DARK_GREEN = (0, 100, 0)
BLACK = (0, 0, 0)
CYAN = (0, 255, 255)


@unique
class GameState(Enum):
    TITLE_SCREEN = 0
    SINGLE_PLAYER = 1
    TWO_PLAYERS = 2
    PAUSED = 3
    GAME_OVER = 4


WINNING_SCORE: int = 11
PADDLE_SPEED: float = 200.0
BASE_BALL_SPEED: float = 150.0
PADDLE_WIDTH, PADDLE_HEIGHT = 20, 100
BALL_SIZE: int = 20
left_paddle_y: float = SCREEN_HEIGHT // 2 - PADDLE_HEIGHT // 2
right_paddle_y: float = SCREEN_HEIGHT // 2 - PADDLE_HEIGHT // 2
ball_x: float = SCREEN_WIDTH // 2 - BALL_SIZE // 2
ball_y: float = SCREEN_HEIGHT // 2 - BALL_SIZE // 2
ball_speed_x, ball_speed_y = 0, 0
left_score, right_score = 0, 0

current_state = GameState.TITLE_SCREEN
game_mode = GameState.SINGLE_PLAYER
game_winner: str
font_small = pygame.font.SysFont("Times New Roman", 24)
font_medium = pygame.font.SysFont("Times New Roman", 36)
font_large = pygame.font.SysFont("Times New Roman", 48)


def reset_ball() -> None:
    global ball_x, ball_y, ball_speed_x, ball_speed_y
    ball_x = SCREEN_WIDTH // 2 - BALL_SIZE // 2
    ball_y = SCREEN_HEIGHT // 2 - BALL_SIZE // 2
    ball_speed_x = BASE_BALL_SPEED * choice([-1, 1])
    ball_speed_y = BASE_BALL_SPEED * (uniform(-0.6, 0.6))


def reset_game() -> None:
    global left_paddle_y, right_paddle_y, left_score, right_score, \
        ball_x, ball_y, ball_speed_x, ball_speed_y

    left_paddle_y = SCREEN_HEIGHT // 2 - PADDLE_HEIGHT // 2
    right_paddle_y = SCREEN_HEIGHT // 2 - PADDLE_HEIGHT // 2
    left_score = 0
    right_score = 0
    reset_ball()


def update_ai(dt: float, error_margin: float = 0.2, sensitivity: float = 0.3) -> None:
    global right_paddle_y

    if current_state != GameState.SINGLE_PLAYER:
        return

    predicted_y: float = ball_y
    if ball_speed_x > 0:
        time_to_reach: float = \
            (SCREEN_WIDTH - 10 - PADDLE_WIDTH - ball_x) / ball_speed_x
        predicted_y = ball_y + ball_speed_y * time_to_reach
        predicted_y += (random() - 0.5) * error_margin * PADDLE_HEIGHT

    target_y: float = predicted_y - PADDLE_HEIGHT / 2.0
    target_y = max(0, min(SCREEN_HEIGHT - PADDLE_HEIGHT, target_y))

    if abs(right_paddle_y - target_y) > 1:
        delta_y: float = PADDLE_SPEED * dt * sensitivity
        right_paddle_y += delta_y if right_paddle_y < target_y else -delta_y


clock = pygame.time.Clock()
running: float = True
while running:
    dt: float = clock.tick(60) / 1000

    for event in pygame.event.get():
        match event.type:
            case pygame.QUIT:
                running = False
            case pygame.KEYDOWN if event.key == pygame.K_p:
                match current_state:
                    case GameState.SINGLE_PLAYER | GameState.TWO_PLAYERS:
                        current_state = GameState.PAUSED
                    case GameState.PAUSED:
                        current_state = game_mode
            case pygame.KEYDOWN if event.key == pygame.K_ESCAPE:
                if current_state in [GameState.GAME_OVER, GameState.PAUSED]:
                    current_state = GameState.TITLE_SCREEN
                else:
                    running = False

    keys = pygame.key.get_pressed()

    if current_state == GameState.GAME_OVER and keys[pygame.K_SPACE]:
        current_state = game_mode
        reset_game()

    if current_state == GameState.TITLE_SCREEN:
        if keys[pygame.K_1]:
            game_mode = GameState.SINGLE_PLAYER
            current_state = GameState.SINGLE_PLAYER
            reset_game()
        elif keys[pygame.K_2]:
            game_mode = GameState.TWO_PLAYERS
            current_state = GameState.TWO_PLAYERS
            reset_game()

    if current_state in [GameState.SINGLE_PLAYER, GameState.TWO_PLAYERS]:
        if keys[pygame.K_w]:
            left_paddle_y -= PADDLE_SPEED * dt
        if keys[pygame.K_s]:
            left_paddle_y += PADDLE_SPEED * dt

        if current_state == GameState.TWO_PLAYERS:
            if keys[pygame.K_UP]:
                right_paddle_y -= PADDLE_SPEED * dt
            if keys[pygame.K_DOWN]:
                right_paddle_y += PADDLE_SPEED * dt

        if current_state == GameState.SINGLE_PLAYER:
            update_ai(dt)

        left_paddle_y = max(
            0, min(SCREEN_HEIGHT - PADDLE_HEIGHT, left_paddle_y))
        right_paddle_y = max(
            0, min(SCREEN_HEIGHT - PADDLE_HEIGHT, right_paddle_y))

        ball_x += ball_speed_x * dt
        ball_y += ball_speed_y * dt

        if ball_y <= 0:
            ball_y = 0
            ball_speed_y = -ball_speed_y
        elif ball_y >= SCREEN_HEIGHT - BALL_SIZE:
            ball_y = SCREEN_HEIGHT - BALL_SIZE
            ball_speed_y = -ball_speed_y

        if (ball_x <= PADDLE_WIDTH and
            ball_x + BALL_SIZE >= 0 and
            ball_y + BALL_SIZE >= left_paddle_y and
            ball_y <= left_paddle_y + PADDLE_HEIGHT and
                ball_speed_x < 0):

            hit_pos = ball_y + BALL_SIZE // 2 - left_paddle_y
            normalized = (hit_pos / PADDLE_HEIGHT) - 0.5

            ball_speed_x = -ball_speed_x * 1.1
            ball_speed_y += normalized * 200

            ball_x = PADDLE_WIDTH

        if (ball_x + BALL_SIZE >= SCREEN_WIDTH - PADDLE_WIDTH and
            ball_x <= SCREEN_WIDTH - PADDLE_WIDTH + BALL_SIZE and
            ball_y + BALL_SIZE >= right_paddle_y and
            ball_y <= right_paddle_y + PADDLE_HEIGHT and
                ball_speed_x > 0):

            hit_pos = ball_y + BALL_SIZE // 2 - right_paddle_y
            normalized = (hit_pos / PADDLE_HEIGHT) - 0.5

            ball_speed_x = -ball_speed_x * 1.1
            ball_speed_y += normalized * 200

            ball_x = SCREEN_WIDTH - PADDLE_WIDTH - BALL_SIZE

        ball_speed_y = max(-300, min(300, ball_speed_y))

        if ball_x < 0:
            right_score += 1
            if right_score >= WINNING_SCORE and abs(left_score - right_score) >= 2:
                game_winner = "COMPUTER" if \
                    current_state == GameState.SINGLE_PLAYER else "PLAYER 2"

                current_state = GameState.GAME_OVER
            else:
                reset_ball()
        elif ball_x > SCREEN_WIDTH:
            left_score += 1
            if left_score >= WINNING_SCORE and abs(left_score - right_score) >= 2:
                game_winner = "PLAYER 1"
                current_state = GameState.GAME_OVER
            else:
                reset_ball()

    screen.fill(BLACK)

    def center_text_y(text: pygame.Surface) -> float:
        return SCREEN_WIDTH // 2 - text.get_width() // 2

    if current_state == GameState.TITLE_SCREEN:
        title_text = font_large.render("ATARI PONG CLONE", True, YELLOW)
        screen.blit(title_text, (center_text_y(title_text), 10))

        key1_text = font_small.render(
            "Press '1' for single player, and '2' for two players.", True, WHEAT)
        screen.blit(key1_text, (center_text_y(key1_text), 100))

        paddle_title = font_small.render("- PADDLE MOVEMENT -", True, WHITE)
        screen.blit(paddle_title, (center_text_y(paddle_title), 165))

        p1_text = font_small.render(
            "Player 1: Move with 'W' and 'S' keys.", True, WHITE)
        screen.blit(p1_text, (center_text_y(p1_text), 190))

        p2_text = font_small.render(
            "Player 2: Move with up and down arrows.", True, WHITE)
        screen.blit(p2_text, (center_text_y(p2_text), 215))

        playing_title = font_small.render("- WHILE PLAYING -", True, WHITE)
        screen.blit(playing_title, (center_text_y(paddle_title), 300))

        p_text = font_small.render(
            "Press 'P' to pause, and 'ESC' to exit.", True, WHITE)
        screen.blit(p_text, (SCREEN_WIDTH // 2 - p_text.get_width() // 2, 325))

    elif current_state in [GameState.SINGLE_PLAYER, GameState.TWO_PLAYERS]:
        for y in range(0, SCREEN_HEIGHT, 20):
            pygame.draw.line(screen, WHITE, (SCREEN_WIDTH //
                             2, y), (SCREEN_WIDTH // 2, y + 10))

        pygame.draw.rect(screen, WHITE, (0, left_paddle_y,
                         PADDLE_WIDTH, PADDLE_HEIGHT))
        pygame.draw.rect(screen, WHITE, (SCREEN_WIDTH - PADDLE_WIDTH,
                         right_paddle_y, PADDLE_WIDTH, PADDLE_HEIGHT))
        pygame.draw.rect(screen, WHITE, (ball_x, ball_y, BALL_SIZE, BALL_SIZE))

        left_score_text = font_large.render(f"{left_score:02d}", True, WHITE)
        screen.blit(left_score_text, (SCREEN_WIDTH // 2 - 60, 10))

        right_score_text = font_large.render(f"{right_score:02d}", True, WHITE)
        screen.blit(right_score_text, (SCREEN_WIDTH // 2 + 10, 10))

        mode_text = "SINGLE PLAYER" if current_state == GameState.SINGLE_PLAYER else "TWO PLAYERS"
        mode_render = font_small.render(f"MODE: {mode_text}", True, CYAN)
        screen.blit(mode_render, (10, SCREEN_HEIGHT - 30))

    elif current_state == GameState.PAUSED:
        screen.fill(BLUE)
        paused_text = font_large.render("GAME PAUSED", True, YELLOW)
        screen.blit(paused_text, (center_text_y(
            paused_text), SCREEN_HEIGHT // 2 - 50))

        resume_text = font_small.render("PRESS 'P' TO RESUME", True, WHITE)
        screen.blit(resume_text, (center_text_y(
            resume_text), SCREEN_HEIGHT // 2 + 30))

        menu_text = font_small.render("PRESS 'ESC' FOR MENU", True, WHITE)
        screen.blit(menu_text, (center_text_y(
            menu_text), SCREEN_HEIGHT // 2 + 60))

    elif current_state == GameState.GAME_OVER:
        screen.fill(DARK_RED if game_winner == "COMPUTER" else DARK_GREEN)
        winner_text = font_large.render(f"{game_winner} WINS!", True, YELLOW)
        screen.blit(winner_text, (center_text_y(
            winner_text), SCREEN_HEIGHT // 3))

        restart_text = font_small.render("PRESS SPACE TO RESTART", True, WHITE)
        screen.blit(restart_text, (center_text_y(
            restart_text), SCREEN_HEIGHT // 2 + 20))

        menu_text = font_small.render("PRESS 'ESC' FOR MENU", True, WHITE)
        screen.blit(menu_text, (center_text_y(
            menu_text), SCREEN_HEIGHT // 2 + 50))

    pygame.display.flip()

pygame.quit()
