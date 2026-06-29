using System;
using System.Collections.Generic;
using UnityEngine;

public class GameEventHandler : MonoBehaviour
{
    public static event Action OnGameStart;

	public static event Action OnGamePause;

	public static event Action<GameState> OnGameOver;

	public static event Action<int> OnPointIncrease;

    public static event Action<SFXID> OnSFXPlay;

    public static event Action<Arrow, ArrowRenderer> OnArrowChosen;

    public static void StartGame() => OnGameStart?.Invoke();
    public static void PauseGame() => OnGamePause?.Invoke();
    public static void EndGame(GameState state) => OnGameOver?.Invoke(state);
    public static void IncreasePoint(int digit) => OnPointIncrease?.Invoke(digit);

    public static void PlaySFX(SFXID sfxId) =>
        OnSFXPlay?.Invoke(sfxId);

    public static void TryMoveArrow(Arrow arrowData, ArrowRenderer arrowRenderer) =>
        OnArrowChosen?.Invoke(arrowData, arrowRenderer);
}
