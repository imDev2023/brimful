using UnityEngine;

namespace Brimful
{
    /// <summary>
    /// Marks a collider (on this object or its children) as climbable: the hero grabs on when
    /// pushing into it, and climbs its face. The wall's holds are its look; the climb itself
    /// follows the face.
    /// </summary>
    public class ClimbSurface : MonoBehaviour
    {
        public static bool Is(Collider c) => c != null && c.GetComponentInParent<ClimbSurface>() != null;
    }
}
