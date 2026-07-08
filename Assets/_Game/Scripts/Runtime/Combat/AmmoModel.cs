using System;

namespace Game.Runtime
{
    /// Chargeur + rechargement. Réserve infinie. // TODO(spec-web): vérifier si le web a une réserve finie.
    public class AmmoModel
    {
        readonly float reloadTime;
        float reloadRemaining;

        public int MagSize { get; }
        public int InMag { get; private set; }
        public bool IsReloading { get; private set; }

        public AmmoModel(int magSize, float reloadTime)
        {
            MagSize = Math.Max(1, magSize);
            this.reloadTime = Math.Max(0f, reloadTime);
            InMag = MagSize;
        }

        public bool TryConsume()
        {
            if (IsReloading || InMag <= 0) return false;
            InMag--;
            return true;
        }

        public void StartReload()
        {
            if (IsReloading || InMag == MagSize) return;
            IsReloading = true;
            reloadRemaining = reloadTime;
        }

        public void Tick(float dt)
        {
            if (!IsReloading) return;
            reloadRemaining -= dt;
            if (reloadRemaining <= 0f)
            {
                IsReloading = false;
                InMag = MagSize;
            }
        }

        /// Munitions infinies (entraînement, T24) : remet le chargeur au max, annule un rechargement en cours.
        public void Refill()
        {
            IsReloading = false;
            InMag = MagSize;
        }
    }
}
