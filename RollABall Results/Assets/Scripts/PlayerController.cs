using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // UI Texte für Punktestand und Gewinnanzeige
    public TMP_Text countText;
    public TMP_Text winText;

    // MAGNET POWER UP
    [Header("Magnet PowerUp")]
    public float magnetDuration = 2f;     // Wie lange der Magnet aktiv ist
    public float magnetRadius = 5f;       // Radius, in dem PickUps angezogen werden
    public float magnetPullSpeed = 6f;    // Geschwindigkeit, mit der PickUps angezogen werden

    private bool magnetActive;             // Ist der Magnet aktuell aktiv?
    private Coroutine magnetCoroutine;     // Referenz zur Magnet-Coroutine

    // JUMP (SPRUNG)
    public float jumpForce = 2.5f;         // Stärke des Sprungs
    private bool isGrounded = true;        // Ist der Spieler am Boden?

    // FALLEN (SCHNELLERES FALLEN)
    public float fallMultiplier = 3f;      // Verstärkt die Schwerkraft beim Fallen
    public float lowJumpMultiplier = 2f;   // Macht kurze Sprünge möglich

    // DASH
    public float dashForce = 3f;           // Stärke des Dashs
    public float dashCooldown = 2f;        // Cooldown zwischen zwei Dashs
    private bool canDash = true;           // Darf aktuell gedasht werden?

    // BEWEGUNG
    public float speed = 10.10f;           // Bewegungsgeschwindigkeit
    private Rigidbody rb;                  // Rigidbody des Spielers

    private float movementX;               // Bewegung in X-Richtung
    private float movementY;               // Bewegung in Z-Richtung

    // PUNKTESYSTEM
    private int count;                     // Zähler für eingesammelte PickUps


    void Start()
    {
        count = 0;                         // Startwert des Zählers
        SetCountText();                   // UI aktualisieren
        rb = GetComponent<Rigidbody>();   // Rigidbody holen
        winText.gameObject.SetActive(false); // Gewinntext ausblenden
    }

    // Wird vom Input System aufgerufen, wenn der Spieler sich bewegt
    private void OnMove(InputValue movementValue)
    {
        Vector2 movementVector = movementValue.Get<Vector2>();

        movementX = movementVector.x;
        movementY = movementVector.y;
    }

    // Physik-Update
    void FixedUpdate()
    {
        // Bewegung des Spielers
        Vector3 movement = new Vector3(movementX, 0.0f, movementY);
        rb.AddForce(movement * speed);

        // Schnelleres Fallen
        if (rb.linearVelocity.y < 0)
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * (fallMultiplier - 1), ForceMode.Acceleration);
        }
        // Kürzerer Sprung, wenn Sprungtaste losgelassen wird
        else if (rb.linearVelocity.y > 0 && !Keyboard.current.spaceKey.isPressed)
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1), ForceMode.Acceleration);
        }

        // Wenn Magnet aktiv ist, PickUps anziehen
        if (magnetActive)
        {
            PullPickups();
        }
    }

    // Zeichnet im Scene-View einen Kreis für den Magnet-Radius
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
    }

    // TRIGGER (PICKUPS & MAGNET)
    private void OnTriggerEnter(Collider other)
    {
        // Normales PickUp einsammeln
        if (other.gameObject.CompareTag("PickUp"))
        {
            other.gameObject.SetActive(false);
            count++;
            SetCountText();
        }

        // Magnet PowerUp einsammeln
        if (other.gameObject.CompareTag("Magnet"))
        {
            other.gameObject.SetActive(false);

            if (magnetCoroutine != null)
                StopCoroutine(magnetCoroutine);

            magnetCoroutine = StartCoroutine(MagnetRoutine());
        }
    }

    // SPRINGEN
    private void OnJump(InputValue value)
    {
        if (!isGrounded) return;

        // Y-Geschwindigkeit zurücksetzen, damit jeder Sprung gleich stark ist
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        isGrounded = false;
    }

    // DASH
    private void OnDash(InputValue value)
    {
        if (!canDash) return;

        Vector3 dashDirection = new Vector3(movementX, 0f, movementY);

        // Wenn keine Richtung gedrückt wird → nach vorne dashen
        if (dashDirection == Vector3.zero)
            dashDirection = transform.forward;

        // Aktuelle Geschwindigkeit etwas dämpfen
        rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y, rb.linearVelocity.z * 0.5f);

        rb.AddForce(dashDirection.normalized * dashForce, ForceMode.Impulse);

        StartCoroutine(DashCooldownRoutine());
    }

    // UI UPDATE
    void SetCountText()
    {
        countText.text = "Count: " + count.ToString();

        // Gewinn anzeigen, wenn genug PickUps gesammelt
        if (count >= 12)
        {
            winText.gameObject.SetActive(true);
        }
    }

    // BODEN ERKENNUNG
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    // DASH COOLDOWN
    private IEnumerator DashCooldownRoutine()
    {
        canDash = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // MAGNET ROUTINE
    private IEnumerator MagnetRoutine()
    {
        magnetActive = true;
        yield return new WaitForSeconds(magnetDuration);
        magnetActive = false;
    }

    // PICKUPS ANZIEHEN
    private void PullPickups()
    {
        // Alle Collider im Magnet-Radius finden
        Collider[] hits = Physics.OverlapSphere(transform.position, magnetRadius);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("PickUp")) continue;

            Transform p = hit.transform;
            Vector3 target = transform.position;
            target.y = p.position.y; // nur horizontal ziehen

            // Wenn Rigidbody vorhanden → physikalisch bewegen
            if (hit.attachedRigidbody != null && !hit.attachedRigidbody.isKinematic)
            {
                hit.attachedRigidbody.MovePosition(
                    Vector3.MoveTowards(p.position, target, magnetPullSpeed * Time.fixedDeltaTime)
                );
            }
            // Sonst direkt Transform bewegen
            else
            {
                p.position = Vector3.MoveTowards(p.position, target, magnetPullSpeed * Time.fixedDeltaTime);
            }
        }
    }
}
