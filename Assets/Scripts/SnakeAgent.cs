using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class SnakeAgent : Agent
{
    [SerializeField] private float MoveSpeed = 1f;
    [SerializeField] private float TurnSpeed = 1f;

    [Space]
    [SerializeField] private GameObject BodyPartPrefab;
    [SerializeField] private int InitialBodyParts = 3;
    [SerializeField] private float TimeBetweenBodyParts = 0.15f;

    [Space]
    [SerializeField] private float MinFruitDistance = 1f;
    [SerializeField] private Transform Fruit;

    [Space]
    [SerializeField] private LayerMask WallLayer;
    [SerializeField] private LayerMask BodyPartLayer;

    [Space]
    [SerializeField] private CarriculumMode Carriculum;

    private enum CarriculumMode
    {
        ToFirstApple,
        ToFifthApple,
        Infinite
    }

    public readonly List<SnakeMarkerManager> SnakeParts = new();
    private float _bodyPartTimer;
    private int _bodyPartsRemaining;

    private SnakeMarkerManager _headPart;

    private int _appleCounter = 0;
    private float _lastAppleTime = 0f;

    private void Awake()
    {
        _headPart = GetComponent<SnakeMarkerManager>();
    }

    // Funkcja wywoływana przy starcie symulacji
    public override void OnEpisodeBegin()
    {
        RandomizeSpawn();
        RandomizeFruitPosition();

        for (int i = 1; i < SnakeParts.Count; i++)
        {
            Destroy(SnakeParts[i].gameObject);
        }

        SnakeParts.Clear();

        _bodyPartsRemaining = InitialBodyParts;
        _bodyPartTimer = 0f;

        _appleCounter = 0;
        _lastAppleTime = Time.time;
    }

    // Funkcja wywoływana przy każdym kroku symulacji
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition / 10);
        sensor.AddObservation(transform.right);
        sensor.AddObservation(Fruit.localPosition / 10);
        sensor.AddObservation(Vector3.Distance(transform.localPosition, Fruit.localPosition) / 20);

        // distance to wall/body part (danger)
        var hit = Physics2D.Raycast(transform.localPosition, transform.right, 10, WallLayer);
        sensor.AddObservation(hit.distance / 10);

        hit = Physics2D.Raycast(transform.localPosition, transform.right, 10, BodyPartLayer);
        sensor.AddObservation(hit.distance / 10);
    }

    // Funckja wywoływana przy każdym kroku symulacji
    public override void OnActionReceived(ActionBuffers actions)
    {
        // 2 lewo
        // 1 prawo
        // 0 nic
        float turnDir = actions.DiscreteActions[0];

        turnDir = turnDir switch
        {
            2 => -1,
            1 => 1,
            _ => 0
        };

        // Obracamy agenta i przesuwamy go
        transform.Rotate(Vector3.forward, -turnDir * TurnSpeed * Time.fixedDeltaTime);
        transform.localPosition += transform.right * MoveSpeed * Time.fixedDeltaTime;

        foreach (var part in SnakeParts)
        {
            part.AddMarker();
        }

        TryAddBodyPart();
        MoveBodyParts();

        AddReward(-0.001f);

        // Jeśli agent nie zje jabłka przez 10 sekund, to kończymy epizod
        // if (Time.time - _lastAppleTime > 45f)
        // {
        //     AddReward(-1f);
        //     EndEpisode();
        // }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Wall") || other.gameObject.CompareTag("BodyPart"))
        {
            AddReward(-1f);
            EndEpisode();
        }

        if (other.CompareTag("Fruit"))
        {
            _appleCounter++;
            _lastAppleTime = Time.time;
            AddReward(1f);

            if (Carriculum == CarriculumMode.ToFirstApple)
            {
                EndEpisode();
                return;
            }

            if (Carriculum == CarriculumMode.ToFifthApple && _appleCounter >= 5)
            {
                EndEpisode();
            }

            RandomizeFruitPosition();

            _bodyPartsRemaining++;

            if (SnakeParts.Count >= 1)
                SnakeParts[^1].ResetMarkers();
        }
    }

    // Funkcja używana do testowania agenta przyciskami klawiatury
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.DiscreteActions;

        var input = Input.GetAxis("Horizontal");
        continuousActions[0] = input switch
        {
            < 0 => 2,
            > 0 => 1,
            _   => 0
        };
    }

    private void AddHeadPart()
    {
        SnakeParts.Add(_headPart);
        _headPart.ResetMarkers();
    }

    private void TryAddBodyPart()
    {
        if (SnakeParts.Count == 0)
        {
            AddHeadPart();
            return;
        }

        if (_bodyPartsRemaining <= 0) return;

        _bodyPartTimer += Time.deltaTime;

        if (_bodyPartTimer >= TimeBetweenBodyParts)
        {
            _bodyPartTimer = 0f;
            _bodyPartsRemaining--;

            var lastPart = SnakeParts[^1];
            var newBodyPart = Instantiate(BodyPartPrefab, Vector3.zero, Quaternion.identity, transform.parent).GetComponent<SnakeMarkerManager>();

            newBodyPart.transform.localPosition = lastPart.Markers[0].position;
            newBodyPart.transform.localRotation = lastPart.Markers[0].rotation;

            newBodyPart.ResetMarkers();

            SnakeParts.Add(newBodyPart);
        }
    }

    private void MoveBodyParts()
    {
        if (SnakeParts.Count <= 1) return;

        for (int i = SnakeParts.Count - 1; i > 0; i--)
        {
            var previousPartMarker = SnakeParts[i - 1].Markers[0];
            SnakeParts[i].transform.localPosition = previousPartMarker.position;
            SnakeParts[i].transform.localRotation = previousPartMarker.rotation;

            SnakeParts[i - 1].Markers.RemoveAt(0);
        }
    }

    private const int MAX_ITERATIONS = 100;
    private void RandomizeFruitPosition()
    {
        var fruitDistance = -1f;
        var iterations = 0;

        while (fruitDistance < MinFruitDistance && iterations < MAX_ITERATIONS)
        {
            Fruit.localPosition = new Vector3(UnityEngine.Random.Range(-10, 10), UnityEngine.Random.Range(-10, 10), 0);
            // get closest distance to body parts
            fruitDistance = float.MaxValue;

            foreach (var part in SnakeParts)
            {
                var distance = Vector3.Distance(part.transform.localPosition, Fruit.localPosition);
                fruitDistance = Mathf.Min(fruitDistance, distance);
            }

            iterations++;
        }
    }

    private void RandomizeSpawn()
    {
        transform.localPosition = new Vector3(UnityEngine.Random.Range(-8, 8), UnityEngine.Random.Range(-8, 8), 0);
        transform.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 360));
    }
}
