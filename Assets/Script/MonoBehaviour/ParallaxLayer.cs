using UnityEngine;

//[ExecuteInEditMode]
public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 1f)] public float parallaxFactor;
    [Range(0f, 1f)] public float parallaxFactorY;

    public void Move(Vector3 delta)
    {
        Vector3 newPos = transform.position;
        newPos.x -= delta.x * parallaxFactor;
        newPos.y -= delta.y * parallaxFactorY;

        transform.position = newPos;
    }

}
