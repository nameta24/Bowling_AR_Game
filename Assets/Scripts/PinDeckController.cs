// PinDeckController.cs

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class PinDeckController : MonoBehaviour
{
    [SerializeField] private Transform _arCamera;
    [SerializeField] private GameObject _bowlingLanePrefab;
    [SerializeField] private GameObject _pinDeckPrefab;
    [SerializeField] private PlaneFinderBehaviour _planeFinder;
    [SerializeField] private GameState _gameState;

    private GameObject _pinDeckClone;
    private Transform _pinDeckSpawnPoint;

    private bool _pinDeckCreated;
    private Pin[] _pins;

    void OnEnable()
    {
        _gameState.OnBallPlayEnd.AddListener(StartBallPlayEnded);
        _gameState.OnResettingDeck.AddListener(StartPlaceNewDeckOnLane);
    }

    void OnDisable()
    {
        _gameState.OnBallPlayEnd.RemoveListener(StartBallPlayEnded);
        _gameState.OnResettingDeck.RemoveListener(StartPlaceNewDeckOnLane);
    }

    private void Awake()
    {
        // set ar camera position for editor testing
#if UNITY_EDITOR
        _arCamera.transform.position = new Vector3(0, 1.4f, 4);
        _arCamera.transform.eulerAngles = new Vector3(
            _arCamera.transform.eulerAngles.x,
            _arCamera.transform.eulerAngles.y + 180,
            _arCamera.transform.eulerAngles.z
        );
#endif
    }

    // Update is called once per frame
    void Update()
    {
        // For testing purposes, start and create pin deck on mouse click
#if UNITY_EDITOR
        if (!_pinDeckCreated)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _pinDeckCreated = true;
                CreatePinDeck();
                Debug.Log("Mouse Left Button Clicked, CreatePinDeck()");
            }
        }
#endif

    }

    public void CreatePinDeck()
    {
        StartCoroutine(SetupBowlingLaneRoutine());
    }

    private IEnumerator SetupBowlingLaneRoutine()
    {
        Transform defaultPlaneIndicator = _planeFinder.PlaneIndicator.transform;
        Vector3 directionTowardsCamera = defaultPlaneIndicator.position + _arCamera.position;

#if UNITY_EDITOR
        directionTowardsCamera = defaultPlaneIndicator.position - _arCamera.position;
#endif

        directionTowardsCamera.y = 0;

        Quaternion lookRotation = Quaternion.LookRotation(-directionTowardsCamera, Vector3.up);

        GameObject bowlingLaneClone = Instantiate(_bowlingLanePrefab, defaultPlaneIndicator.position, lookRotation);

        _pinDeckSpawnPoint = bowlingLaneClone.transform.Find("PinDeckSpawnPoint");

        _pinDeckClone = Instantiate(_pinDeckPrefab, _pinDeckSpawnPoint.position, _pinDeckSpawnPoint.rotation);

        yield return new WaitForSeconds(1);

        _gameState.CurrentGameState = GameState.GameStateEnum.SetupBalls;

        _pins = _pinDeckClone.transform.GetComponentsInChildren<Pin>();

        LowerPinDeck();

        yield return new WaitForSeconds(1);
    }

    void LowerPinDeck()
    {
        foreach (Pin pin in _pins)
        {
            pin.StartLowerPin();
        }
    }

    void RaisePinDeck()
    {
        foreach (Pin pin in _pins)
        {
            if (!pin.IsPinDown()) pin.StartRaisePin();
        }
    }

    void StartBallPlayEnded()
    {
        Debug.Log("BallPlayEnded()");

        StartCoroutine(BallPlayEnded());
    }

    IEnumerator BallPlayEnded()
    {
        foreach (Pin pin in _pins)
        {
            if (pin.IsPinDown())
            {
                _gameState.Score++;

                _gameState.StrikeCounter++;
            }
        }

        if (_gameState.StrikeCounter == 10)
        {
            _gameState.CurrentGameState = GameState.GameStateEnum.StrikeAchieved;

            _gameState.Score += _gameState.StrikeExtraPoints;

            yield return new WaitForSeconds(2);
        }

        _gameState.StrikeCounter = 0;


        RaisePinDeck();

        yield return new WaitForSeconds(2);

        _gameState.CurrentGameState = GameState.GameStateEnum.TurnEnd;
    }


    void StartPlaceNewDeckOnLane()
    {
        Debug.Log("PLACE NEW DECK ON LANE");

        StartCoroutine(PlaceNewDeckOnLane());
    }

    IEnumerator PlaceNewDeckOnLane()
    {
        foreach (Pin pin in _pins)
        {
            pin.Reset();

            pin.StartLowerPin();
        }

        yield return new WaitForSeconds(2);

        _gameState.CurrentGameState = GameState.GameStateEnum.ReadyToThrow;
    }

}