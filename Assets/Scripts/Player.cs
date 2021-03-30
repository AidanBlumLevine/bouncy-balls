using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float radius = .1f;
    public Vector2 velocity;
    Vector2 mouseDownAt;
    void Start()
    {
        velocity = new Vector2(0, 0);
    }

    void Update()
    {
        Time.timeScale = Mathf.Lerp(Time.timeScale, Input.GetMouseButton(0) ? .1f : 1, Time.deltaTime / Time.timeScale * 2);
        if (Input.GetMouseButton(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Debug.DrawLine(mouseDownAt, mousePos);
        }
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (((Vector2)transform.position - mousePos).sqrMagnitude < radius * radius)
            {
                mouseDownAt = mousePos;
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            velocity += (mousePos - mouseDownAt).normalized * 15;
        }
    }

    void FixedUpdate()
    {
        transform.position += (Vector3)velocity * Time.fixedDeltaTime;
        velocity += Time.fixedDeltaTime * Vector2.down * 5;
    }
}
