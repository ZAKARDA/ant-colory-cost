using System.Collections;
using UnityEngine;

namespace Antchemy
{
    public class Tile : MonoBehaviour
    {
        public int CubeDataId { get; private set; }
        public int ColorIndex { get; private set; }
        public bool IsDestroyed { get; private set; }
        public bool IsReserved { get; private set; }
        public bool IsHidden { get; private set; }
        public void ConfigureHidden(bool hidden)
        {
            IsHidden=hidden;
            GameplayVisuals.Paint(GetComponent<Renderer>(),hidden?new Color(.73f,.77f,.81f):GameplayVisuals.Pastel(ColorIndex));
        }
        public void Reveal() { if(IsHidden) ConfigureHidden(false); }
        private PictureGrid _owner;

        public void Initialize(int cubeId, int colorIdx)
        {
            CubeDataId = cubeId;
            ColorIndex = colorIdx;
            IsDestroyed = false;
            IsReserved = false;
            _owner = GetComponentInParent<PictureGrid>();
            GetComponent<MeshFilter>().sharedMesh = GameplayVisuals.CubeMesh;
            GameplayVisuals.Paint(GetComponent<Renderer>(), GameplayVisuals.Pastel(colorIdx));
        }
        public bool TryReserve()
        {
            if (IsReserved || IsDestroyed) return false;
            IsReserved = true;
            return true;
        }
        public void ReleaseReservation() { if (!IsDestroyed) IsReserved = false; }
        public void HitAndDestroy()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            IsReserved = false;
            _owner?.OnCubeDestroyed(this);
            AudioManager.Instance?.PlaySfx("cube_break_1");
            StartCoroutine(Shrink());
        }
        private IEnumerator Shrink()
        {
            Vector3 scale = transform.localScale;
            float elapsed = 0;
            while (elapsed < .12f)
            {
                elapsed += Time.deltaTime;
                transform.localScale = scale * Mathf.Max(0, 1-elapsed/.12f);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
