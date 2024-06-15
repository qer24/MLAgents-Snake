using System;
using UnityEngine;

public class SnakeTail : MonoBehaviour
{
    [SerializeField] private SnakeAgent Snake;

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        _lineRenderer.positionCount = Snake.SnakeParts.Count;

        for (int i = 0; i < Snake.SnakeParts.Count; i++)
        {
            _lineRenderer.SetPosition(i, Snake.SnakeParts[i].transform.position);
        }
    }
}
