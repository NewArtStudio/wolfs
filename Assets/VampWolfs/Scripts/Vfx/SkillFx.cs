using UnityEngine;

namespace VampWolfs.Vfx
{
    /// <summary>
    /// "Receitas" visuais de cada habilidade (VampWolfs, out/2026), montadas com as peças do VfxSystem copiado do Space:
    /// faíscas, brilhos, fumaça, fogo, morcegos, anéis e cones no chão, manchas, feixes, clarões de luz e tremor.
    /// Cada habilidade escolhe a receita pelo campo Ability.Fx (ex.: "blood_bolt", "claws"); sem Fx usa uma genérica na cor.
    /// Só MOSTRA: dano, alcance e acerto continuam nas habilidades. Todos os métodos devolvem false sem VFX
    /// (aí quem chamou usa o efeito simples antigo, Effects).
    /// </summary>
    public static class SkillFx
    {
        static VfxSystem V { get { return VfxHub.Fx; } }

        // Paleta (HDR: acima de 1 brilha com o bloom)
        public static readonly Color Blood = new Color(2.2f, 0.12f, 0.16f);
        public static readonly Color BloodDark = new Color(0.9f, 0.02f, 0.06f);
        public static readonly Color BloodDecal = new Color(0.32f, 0.02f, 0.03f, 0.85f);
        public static readonly Color Violet = new Color(1.5f, 0.45f, 2.1f);
        public static readonly Color Magenta = new Color(2.2f, 0.4f, 1.9f);
        public static readonly Color MistSmoke = new Color(0.42f, 0.32f, 0.58f, 0.7f);
        public static readonly Color BatColor = new Color(0.08f, 0.03f, 0.06f, 0.95f);
        public static readonly Color Amber = new Color(2.2f, 1.2f, 0.35f);
        public static readonly Color ClawWhite = new Color(2.2f, 1.9f, 1.4f);
        public static readonly Color Moon = new Color(2.4f, 0.35f, 0.18f);
        public static readonly Color HowlPale = new Color(1.6f, 1.55f, 1.05f);
        public static readonly Color Steel = new Color(0.8f, 1.1f, 1.6f);
        public static readonly Color Earth = new Color(0.36f, 0.27f, 0.2f, 0.8f);

        static float Size { get { return CharacterFactory.SizeMultiplier; } }
        public static Vector3 Chest(CombatUnit u) { return u.Position + Vector3.up * 1.3f * Size; }
        public static Vector3 Hand(CombatUnit u) { return u.Position + u.transform.forward * 0.55f * Size + Vector3.up * 1.25f * Size; }
        static float Yaw(Vector3 dir) { return Mathf.Atan2(dir.x, dir.z); }
        static Color Hdr(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, 1f); }
        static float Rnd(float a, float b) { return Random.Range(a, b); }

        /// <summary>Sorteio "por segundo" (igual em 60 e em 240 fps): true em média ratePerSecond vezes por segundo.</summary>
        public static bool Every(float ratePerSecond) { return Random.value < ratePerSecond * Time.deltaTime; }

        /* ======================= conjuração (carga na mão) ======================= */

        /// <summary>Cor da carga/brilho na mão enquanto conjura.</summary>
        public static Color ChargeColor(Ability a)
        {
            switch (a.Fx)
            {
                case "blood_bolt": case "lance": case "nova": case "feast": return Blood;
                case "lance_ex": case "gaze": return Magenta;
                case "mist_step": case "bats": return Violet;
                case "claws": case "rush": case "pounce": return Amber;
                case "rend": case "blood_moon": return Moon;
                case "howl": return HowlPale;
                case "hide": case "mirror": return Steel;
            }
            return Hdr(ColorOf(a), 1.8f);
        }

        static Color ColorOf(Ability a)
        {
            var p = a as ProjectileAbility; if (p != null) return p.Color;
            var m = a as MeleeAbility; if (m != null) return m.Color;
            var r = a as AreaAbility; if (r != null) return r.Color;
            var d = a as DashAbility; if (d != null) return d.Color;
            var b = a as BuffAbility; if (b != null) return b.Color;
            var z = a as ZoneAbility; if (z != null) return z.Color;
            var t = a as TrapAbility; if (t != null) return t.Color;
            return new Color(1f, 0.8f, 0.5f);
        }

        /// <summary>Partículas de carga enquanto conjura (chamado todo quadro pelo UnitFx, k = progresso 0..1).</summary>
        public static void CastTick(CombatUnit u, Ability a, float k)
        {
            if (V == null) return;
            var c = ChargeColor(a);
            var hand = Hand(u);
            if (Every(30f + k * 30f))
            {
                var off = Random.onUnitSphere * (0.7f - 0.5f * k) * Size;
                V.Glows.Emit(hand + off, -off * 2.5f, Rnd(0.1f, 0.18f) * Size, 0.25f, c);   // energia sendo puxada para a mão
            }
            if (a.Fx == "feast" || a.Fx == "blood_moon")
            {
                if (Every(24f)) V.Embers.Emit(u.Position + new Vector3(Rnd(-1.2f, 1.2f), 0.1f, Rnd(-1.2f, 1.2f)) * Size, Vector3.up * Rnd(1f, 3f), Rnd(0.06f, 0.11f), Rnd(0.5f, 0.9f), c);
            }
        }

        /* ======================= projéteis ======================= */

        /// <summary>Visual do projétil (brilho no núcleo + rastro), chamado todo quadro pelo ProjectileFx.</summary>
        public static void ProjectileTrail(ProjectileAbility a, Vector3 from, Vector3 to, Vector3 dir, float scale)
        {
            if (V == null) return;
            float dist = (to - from).magnitude;
            int n = Mathf.Clamp(Mathf.CeilToInt(dist * 3f), 1, 10);
            for (int i = 0; i < n; i++)
            {
                var p = Vector3.Lerp(from, to, i / (float)n);
                switch (a.Fx)
                {
                    case "lance":
                        V.Glows.Emit(p + Random.insideUnitSphere * 0.08f, -dir * 1.5f, Rnd(0.2f, 0.32f) * scale, 0.22f, Blood);
                        if (i % 2 == 0) V.Embers.Emit(p, Random.insideUnitSphere * 1.2f - dir * 2f, 0.07f, Rnd(0.3f, 0.6f), BloodDark * 2f);
                        if (i % 3 == 0) V.Smoke.Emit(p, Random.insideUnitSphere * 0.3f, Rnd(0.35f, 0.55f) * scale, 0.6f, new Color(0.25f, 0.02f, 0.04f, 0.45f), -1f, 30f);
                        break;
                    case "lance_ex":
                        V.Glows.Emit(p, -dir * 1.5f, Rnd(0.22f, 0.34f) * scale, 0.25f, Magenta);
                        if (i % 2 == 0) V.Shards.Emit(p + Random.insideUnitSphere * 0.15f, Random.insideUnitSphere * 1.5f, Rnd(0.12f, 0.2f), 0.4f, Violet, -1f, 200f);
                        break;
                    case "gaze":
                        float t = Time.time * 18f + i;
                        var side = Vector3.Cross(dir, Vector3.up) * Mathf.Cos(t) * 0.35f + Vector3.up * Mathf.Sin(t) * 0.35f;   // espiral hipnótica
                        V.Glows.Emit(p + side * scale * 2f, Vector3.zero, 0.14f * scale * 2f, 0.3f, Magenta);
                        if (i == 0) V.Glows.Emit(p - side * scale * 2f, Vector3.zero, 0.14f * scale * 2f, 0.3f, Violet);
                        break;
                    case "feast":
                        V.Fire.Emit(p + Random.insideUnitSphere * 0.25f, Random.insideUnitSphere * 0.6f - dir * 2f, Rnd(0.7f, 1.1f), Rnd(0.25f, 0.4f), new Color(1.6f, 0.25f, 0.3f), -1f, 60f);
                        if (i % 2 == 0) V.Glows.Emit(p, -dir, Rnd(0.5f, 0.8f), 0.3f, Blood);
                        if (i == 0 && Random.value < 0.6f) V.BatSwarm(p, -dir + Random.insideUnitSphere, BatColor, 1, 3f, 0.8f, 0.7f, 0.5f);
                        break;
                    default:   // seta de sangue e genéricos: gotas brilhando e caindo
                        var col = a.Fx == "blood_bolt" ? Blood : Hdr(a.Color, 2f);
                        V.Glows.Emit(p + Random.insideUnitSphere * 0.05f, -dir * 0.8f, Rnd(0.14f, 0.22f) * scale * 2.2f, 0.18f, col);
                        if (i % 3 == 0) V.Sparks.Emit(p, Random.insideUnitSphere * 1.2f + Vector3.down * 1.5f, 0.05f, 0.35f, col);
                        break;
                }
            }
        }

        /// <summary>Cor e tamanho do núcleo brilhante do projétil.</summary>
        public static Color CoreColor(ProjectileAbility a)
        {
            switch (a.Fx)
            {
                case "blood_bolt": case "lance": case "feast": return Blood;
                case "lance_ex": case "gaze": return Magenta;
            }
            return Hdr(a.Color, 2f);
        }

        /// <summary>Disparo: clarão na mão, faíscas para a frente.</summary>
        public static bool ProjectileLaunch(CombatUnit u, ProjectileAbility a, Vector3 dir)
        {
            if (V == null) return false;
            var hand = Hand(u); var c = CoreColor(a);
            bool big = a.Fx == "feast" || a.Fx == "lance" || a.Fx == "lance_ex";
            V.Flash(hand, c, big ? 1.4f : 0.7f, 0.12f, big ? 0.8f : 0.3f);
            V.SparkSpray(hand, dir, c, big ? 12 : 5, big ? 9f : 6f, 0.35f);
            if (a.Fx == "feast")
            {
                V.BatSwarm(hand, dir, BatColor, 10, 7f, 0.5f, 1.1f, 0.55f);
                V.Ring(u.Position, Blood, 0.5f, 3.5f, 0.5f, 0.3f, 0.3f);
                V.AddShake(0.25f, true);
            }
            else if (big) V.AddShake(0.06f);
            return true;
        }

        /// <summary>Acerto do projétil (alvo ou parede).</summary>
        public static bool ProjectileHit(ProjectileAbility a, Vector3 p, Vector3 dir, bool wall)
        {
            if (V == null) return false;
            var c = CoreColor(a);
            switch (a.Fx)
            {
                case "lance":
                case "lance_ex":
                    V.Flash(p, c, 1.6f, 0.14f, 0.9f);
                    V.Star(p, c, 1.8f, 0.2f);
                    V.SparkSpray(p, dir, c, 18, 9f, 0.45f, 0.35f, 0.08f);
                    V.SlashAt(p, c, 1.4f, Rnd(0f, 180f));
                    if (a.Fx == "lance_ex") V.ShardBurst(p, Violet, 8, 4f);
                    else V.Decal(p, 1.2f, VfxAssets.Splat, BloodDecal, 7f);
                    V.AddShake(0.08f);
                    break;
                case "gaze":
                    V.Flash(p, c, 1.4f, 0.16f, 0.7f);
                    V.Star(p, Magenta, 2.2f, 0.3f);
                    V.Ring(p, Violet, 0.2f, 1.6f, 0.35f, 0.2f, 0.3f, 0.06f);
                    V.Motes(p, Violet, 8, 0.4f, 0.18f, 0.6f, Vector3.up * 0.8f);
                    break;
                case "feast":
                    V.Flash(p, Blood, 2.6f, 0.2f, 1.4f);
                    V.Star(p, Blood, 2.6f, 0.25f);
                    V.SparkBurst(p, Blood, 26, 8f, 0.45f, 0.09f);
                    V.Ring(p, Blood, 0.3f, 2.6f, 0.45f, 0.35f, 0.4f);
                    V.Decal(p, 2.6f, VfxAssets.Splat, BloodDecal, 9f);
                    V.BatSwarm(p, Vector3.up, BatColor, 8, 4f, 1f, 1f, 0.5f);
                    V.AddShake(0.2f);
                    break;
                default:
                    V.Flash(p, c, 0.9f, 0.1f, 0.4f);
                    V.SparkSpray(p, wall ? -dir : dir, c, 10, 6f, 0.5f, 0.3f, 0.06f);
                    break;
            }
            if (wall) V.Dust(p, 4, 0.3f, 1.5f, 0.6f, Earth);
            return true;
        }

        /* ======================= corpo a corpo ======================= */

        /// <summary>Golpe em arco: varredura no chão + 3 riscos de garra + faíscas.</summary>
        public static bool Melee(CombatUnit u, MeleeAbility a, Vector3 dir)
        {
            if (V == null) return false;
            bool rend = a.Fx == "rend";
            var c = rend ? Moon : (a.Fx == "claws" ? Amber : Hdr(a.Color, 2f));
            V.Cone(u.Position, Yaw(dir), a.Reach, a.Angle, c, rend ? BloodDark * 2f : ClawWhite, 0.28f, 1.8f);
            var front = Chest(u) + dir * a.Reach * 0.55f;
            float baseRot = -Vector3.SignedAngle(Vector3.forward, dir, Vector3.up);
            int slashes = rend ? 4 : 3;
            for (int i = 0; i < slashes; i++)
                V.SlashAt(front + Vector3.up * (i - 1) * 0.22f * Size, rend ? Moon : ClawWhite, (rend ? 2.4f : 1.8f) * Size, baseRot + 35f + i * 8f);
            V.SparkSpray(front, dir, c, rend ? 16 : 8, 7f, 0.6f);
            if (rend) V.AddShake(0.1f);
            return true;
        }

        /// <summary>Acerto de golpe corpo a corpo num alvo (sangue espirrando para longe do atacante).</summary>
        public static void MeleeHit(Vector3 targetChest, Vector3 dir, bool heavy)
        {
            if (V == null) return;
            V.SparkSpray(targetChest, dir, BloodDark * 2f, heavy ? 16 : 10, 7f, 0.5f, 0.35f, 0.08f);
            V.SlashAt(targetChest, ClawWhite, heavy ? 1.6f : 1.1f, Rnd(0f, 180f));
        }

        /* ======================= área ======================= */

        public static bool Area(CombatUnit u, AreaAbility a, Vector3 center)
        {
            if (V == null) return false;
            float R = a.Radius;
            switch (a.Fx)
            {
                case "nova":
                    V.Ring(center, Blood, 0.4f, R, 0.4f, 0.4f, 0.45f);
                    V.Ring(center, BloodDark * 2f, 0.2f, R * 0.8f, 0.55f, 0.25f, 0.2f);
                    V.Flash(center + Vector3.up, Blood, R * 0.9f, 0.18f, 1.2f);
                    RadialSparks(center, Blood, 28, R * 2.4f);
                    V.SmokeBurst(center + Vector3.up * 0.3f, new Color(0.35f, 0.03f, 0.05f, 0.6f), 10, R * 0.4f, 1.2f, R * 0.9f, 1.1f, 0.15f);
                    V.Decal(center, R * 1.6f, VfxAssets.Splat, BloodDecal, 8f);
                    V.AddShake(0.12f);
                    break;
                case "howl":
                    for (int i = 0; i < 3; i++)
                    {
                        float d = i * 0.12f;
                        V.After(d, () => { if (V != null) V.Ring(center, HowlPale, 0.6f, R * 1.1f, 0.5f, 0.2f, 0.1f); });
                    }
                    V.Motes(Chest(u) + u.transform.forward * 0.4f, HowlPale, 12, 0.3f, 0.25f, 0.7f, u.transform.forward * 3f, 1.2f);
                    V.Dust(center, 14, R * 0.5f, R * 1.4f, 1.2f, Earth);
                    V.AddShake(0.14f);
                    break;
                case "blood_moon":
                    V.Flash(center + Vector3.up * 1.5f, Moon, R * 1.2f, 0.25f, 2f);
                    V.Star(center + Vector3.up * 1.5f, Moon, R * 1.2f, 0.35f);
                    for (int i = 0; i < 3; i++) { float d = i * 0.1f; V.After(d, () => { if (V != null) V.Ring(center, Moon, 0.5f, R * (1f + d), 0.6f, 0.4f, 0.4f); }); }
                    RadialSparks(center, Moon, 36, R * 2.6f);
                    V.EmberBurst(center + Vector3.up, Moon, 30, R * 0.5f, 4f);
                    V.Decal(center, R * 2.2f, VfxAssets.Rune, new Color(0.7f, 0.08f, 0.06f, 0.9f), 3f);
                    V.Dust(center, 16, R * 0.5f, R * 1.6f, 1.3f, Earth);
                    V.AddShake(0.3f, true);
                    break;
                default:
                    var c = Hdr(a.Color, 2f);
                    V.Ring(center, c, 0.4f, R, 0.4f, 0.3f, 0.3f);
                    RadialSparks(center, c, 18, R * 2f);
                    break;
            }
            return true;
        }

        static void RadialSparks(Vector3 center, Color c, int n, float speed)
        {
            for (int i = 0; i < n; i++)
            {
                float ang = i / (float)n * Mathf.PI * 2f + Rnd(-0.1f, 0.1f);
                var d = new Vector3(Mathf.Cos(ang), Rnd(0.05f, 0.3f), Mathf.Sin(ang));
                V.Sparks.Emit(center + Vector3.up * 0.5f, d * speed * Rnd(0.6f, 1f), Rnd(0.06f, 0.1f), Rnd(0.3f, 0.45f), c);
            }
        }

        /* ======================= deslocamentos ======================= */

        public static bool DashStart(CombatUnit u, DashAbility a, Vector3 dir)
        {
            if (V == null) return false;
            var p = u.Position;
            switch (a.Fx)
            {
                case "mist_step":
                    V.SmokeBurst(p + Vector3.up * Size, MistSmoke, 10, 0.5f, 1.3f, 1.5f, 0.9f, 0.2f);
                    V.Motes(p + Vector3.up * Size, Violet, 10, 0.6f, 0.2f, 0.6f, Vector3.up * 0.5f);
                    break;
                case "bats":
                    V.SmokeBurst(p + Vector3.up * Size, MistSmoke, 8, 0.5f, 1.3f, 1.5f, 0.9f, 0.2f);
                    V.BatSwarm(p + Vector3.up * Size, dir, BatColor, 22, 9f, 0.45f, 0.9f, 0.55f);
                    break;
                case "pounce":
                    V.Dust(p, 10, 0.6f, 3f, 1.1f, Earth);
                    V.SparkSpray(Chest(u), dir, Amber, 8, 5f, 0.4f);
                    break;
                case "rush":
                    V.Dust(p, 8, 0.5f, 3f, 1f, Earth);
                    break;
                default:
                    V.Ring(p, Hdr(a.Color, 2f), 0.3f, 1.2f, 0.3f);
                    break;
            }
            return true;
        }

        /// <summary>Rastro durante o deslocamento (chamado todo quadro pelo UnitFx).</summary>
        public static void DashTick(CombatUnit u, DashAbility a)
        {
            if (V == null || a == null) return;
            var p = u.Position;
            switch (a.Fx)
            {
                case "mist_step":
                    V.Smoke.Emit(p + Vector3.up * Size * Rnd(0.4f, 1.6f), Random.insideUnitSphere * 0.4f, Rnd(0.8f, 1.2f), 0.7f, MistSmoke, -1f, 30f);
                    V.Glows.Emit(p + Vector3.up * Size, Random.insideUnitSphere, 0.2f, 0.4f, Violet);
                    break;
                case "bats":
                    if (Every(90f)) V.BatSwarm(p + Vector3.up * Size, Random.insideUnitSphere + Vector3.up * 0.3f, BatColor, 2, 3f, 1f, 0.7f, 0.5f);
                    V.Smoke.Emit(p + Vector3.up * Size, Random.insideUnitSphere * 0.4f, 1f, 0.6f, MistSmoke, -1f, 30f);
                    break;
                case "pounce":
                case "rush":
                    V.Smoke.Emit(new Vector3(p.x, 0.25f, p.z), Random.insideUnitSphere * 0.5f, Rnd(0.7f, 1.1f), 0.8f, Earth, -1f, 20f);
                    V.Sparks.Emit(Chest(u), -u.Velocity * 0.1f + Random.insideUnitSphere, 0.06f, 0.25f, Amber);
                    break;
                default:
                    V.Glows.Emit(p + Vector3.up * Size, Vector3.zero, 0.35f, 0.3f, Hdr(a.Color, 1.5f));
                    break;
            }
        }

        public static bool DashLand(CombatUnit u, DashAbility a)
        {
            if (V == null) return false;
            var p = u.Position;
            float R = a.LandingRadius > 0f ? a.LandingRadius : 1f;
            switch (a.Fx)
            {
                case "mist_step":
                case "bats":
                    V.SmokeBurst(p + Vector3.up * Size, MistSmoke, 8, 0.4f, 1.2f, 1.2f, 0.8f, 0.2f);
                    break;
                case "pounce":
                    V.Ring(p, Amber, 0.3f, R * 1.1f, 0.4f, 0.35f, 0.35f);
                    V.Dust(p, 16, R * 0.4f, R * 1.8f, 1.3f, Earth);
                    V.DebrisBurst(p + Vector3.up * 0.2f, new Color(0.3f, 0.22f, 0.15f), 8, 4f, 0.16f);
                    V.Decal(p, R * 1.8f, VfxAssets.Crack, new Color(0.15f, 0.1f, 0.08f, 0.9f), 6f);
                    V.Flash(p + Vector3.up * 0.5f, Amber, R, 0.12f, 0.6f);
                    V.AddShake(0.16f);
                    break;
                default:
                    if (a.LandingRadius > 0f) V.Ring(p, Hdr(a.Color, 2f), 0.3f, R, 0.35f);
                    break;
            }
            return true;
        }

        /* ======================= defesa e reforços ======================= */

        public static bool Buff(CombatUnit u, BuffAbility a)
        {
            if (V == null) return false;
            var c = a.Fx == "hide" ? Steel : Hdr(a.Color, 2f);
            V.Flash(Chest(u), c, 2f, 0.16f, 0.8f);
            V.ShardBurst(Chest(u), c, 10, 4f);
            V.Ring(u.Position, c, 0.3f, 2f, 0.4f, 0.25f, 0.3f);
            return true;
        }

        public static bool CounterStart(CombatUnit u)
        {
            if (V == null) return false;
            V.Flash(Chest(u), Blood, 1.6f, 0.14f, 0.6f);
            V.Ring(u.Position, Blood, 1.6f, 0.6f, 0.3f, 0.2f, 0.2f);   // fecha para dentro: "postura"
            return true;
        }

        /// <summary>Contra-ataque disparou: o espelho de sangue estilhaça.</summary>
        public static bool CounterTrigger(Vector3 pos)
        {
            if (V == null) return false;
            var chest = pos + Vector3.up * 1.3f * Size;
            V.Flash(chest, Blood, 2.6f, 0.2f, 1.4f);
            V.Star(chest, ClawWhite, 2.4f, 0.25f);
            V.ShardBurst(chest, Blood, 18, 6f, 0.28f);
            V.Ring(pos, Blood, 0.4f, 2.6f, 0.45f, 0.35f, 0.3f);
            V.AddShake(0.15f);
            return true;
        }

        /* ======================= zonas e armadilhas ======================= */

        public static void ZoneStart(Vector3 c, ZoneAbility a)
        {
            if (V == null) return;
            var col = Hdr(a.Color, 1.8f);
            V.Ring(c, col, 0.3f, a.Radius, 0.35f, 0.25f, 0.3f);
            V.Flash(c + Vector3.up * 0.5f, col, a.Radius * 0.8f, 0.14f, 0.6f);
        }

        /// <summary>Partículas subindo da zona (chamado todo quadro pela zona).</summary>
        public static void ZoneTick(Vector3 c, ZoneAbility a)
        {
            if (V == null) return;
            float rate = 6f + a.Radius * a.Radius * 2f;
            if (!Every(rate)) return;
            float ang = Rnd(0f, Mathf.PI * 2f), r = a.Radius * Mathf.Sqrt(Rnd(0f, 1f));
            var p = c + new Vector3(Mathf.Cos(ang) * r, 0.1f, Mathf.Sin(ang) * r);
            if (a.HealPerSecond > 0f) V.Pluses.Emit(p, Vector3.up * 1.2f, 0.2f, 0.9f, Hdr(a.Color, 1.6f), 0f, 0f);
            else if (a.Hit.Damage > 0f) V.Embers.Emit(p, Vector3.up * Rnd(1f, 2.5f), Rnd(0.06f, 0.1f), Rnd(0.5f, 0.9f), Hdr(a.Color, 2f));
            else V.Smoke.Emit(p, Vector3.up * 0.3f, Rnd(0.8f, 1.3f), Rnd(0.8f, 1.4f), new Color(a.Color.r * 0.6f, a.Color.g * 0.6f, a.Color.b * 0.6f, 0.45f), -1f, 20f);
        }

        public static bool TrapSnap(Vector3 p, TrapAbility a)
        {
            if (V == null) return false;
            var col = Hdr(a.Color, 2f);
            V.Flash(p + Vector3.up * 0.4f, col, 1.6f, 0.12f, 0.8f);
            V.SparkBurst(p + Vector3.up * 0.3f, new Color(2f, 1.6f, 1f), 14, 4f, 0.35f, 0.07f, 0.5f);
            V.DebrisBurst(p + Vector3.up * 0.2f, new Color(0.3f, 0.28f, 0.26f), 5, 3f, 0.12f);
            V.Ring(p, col, 0.2f, a.Radius * 1.2f, 0.3f, 0.25f, 0.3f);
            if (a.Hit.Damage >= 15f) { V.Explosion(p, a.Radius, new Color(2.2f, 1.1f, 0.4f), true, 0.12f); }
            return true;
        }

        /* ======================= eventos gerais ======================= */

        /// <summary>Sangue ao levar dano (sentido: do atacante para o alvo).</summary>
        public static void Damaged(Vector3 chest, Vector3 fromDir, float amount, Color blood)
        {
            if (V == null) return;
            int n = Mathf.Clamp(Mathf.RoundToInt(4 + amount * 0.5f), 4, 18);
            V.SparkSpray(chest, fromDir + Vector3.up * 0.3f, blood, n, 5f, 0.6f, 0.4f, 0.07f);
            V.Glows.Emit(chest, Vector3.zero, 0.5f, 0.12f, blood);
            if (Random.value < 0.35f) V.Decal(chest - Vector3.up * chest.y + fromDir * 0.5f, Rnd(0.6f, 1.1f), VfxAssets.Splat, BloodDecal, 7f);
        }

        public static void Healed(Vector3 pos, float amount, Color c)
        {
            if (V == null || amount < 1f) return;
            V.PlusBurst(pos + Vector3.up * 0.8f * Size, c, Mathf.Clamp(Mathf.RoundToInt(amount * 0.3f), 2, 8), 0.5f * Size);
        }

        public static void VampireDeath(Vector3 pos)
        {
            if (V == null) return;
            var c = pos + Vector3.up * Size;
            V.SmokeBurst(c, new Color(0.12f, 0.08f, 0.1f, 0.85f), 16, 0.6f, 1.6f, 1.8f, 1.8f, 0.8f);   // vira cinzas
            V.EmberBurst(c, Blood, 26, 0.6f, 2.5f);
            V.BatSwarm(c, Vector3.up, BatColor, 16, 6f, 1f, 1.3f, 0.55f);
            V.Flash(c, Blood, 2.5f, 0.2f, 1f);
            V.Decal(pos, 2.2f, VfxAssets.Scorch, new Color(0.2f, 0.12f, 0.12f, 0.9f), 12f);
            V.AddShake(0.2f, true);
        }

        public static void BeastDeath(Vector3 pos)
        {
            if (V == null) return;
            V.Gibs(pos, 1.4f * Size, BloodDark * 1.5f);
            V.Dust(pos, 12, 0.8f, 2.5f, 1.2f, Earth);
            V.Decal(pos, 2.6f, VfxAssets.Splat, BloodDecal, 12f);
            V.AddShake(0.2f, true);
        }

        public static bool SheepDeath(Vector3 pos)
        {
            if (V == null) return false;
            var c = pos + Vector3.up * 0.7f;
            V.SmokeBurst(c, new Color(0.95f, 0.93f, 0.88f, 0.85f), 10, 0.4f, 0.9f, 2.2f, 0.9f, 0.4f);   // lã
            V.SparkBurst(c, BloodDark * 2f, 10, 3.5f, 0.4f, 0.08f, 0.4f);
            V.Decal(pos, 1.4f, VfxAssets.Splat, BloodDecal, 10f);
            return true;
        }

        /// <summary>Subiu de nível: coluna de luz violeta e faíscas.</summary>
        public static void LevelUp(Vector3 pos)
        {
            if (V == null) return;
            var c = new Color(1.6f, 1.1f, 2.4f);
            V.DropBeam(pos, c);
            V.SparkBurst(pos + Vector3.up * Size, c, 24, 5f, 0.6f, 0.08f, 0.9f);
            V.Ring(pos, c, 0.3f, 2.5f, 0.6f, 0.25f, 0.3f);
        }

        public static bool BagDropped(Vector3 pos)
        {
            if (V == null) return false;
            V.DropBeam(pos, new Color(2.2f, 1.6f, 0.4f));
            return true;
        }

        public static bool BagPicked(Vector3 pos)
        {
            if (V == null) return false;
            var c = pos + Vector3.up * 0.6f;
            V.Flash(c, new Color(2.2f, 1.6f, 0.4f), 1.6f, 0.16f, 0.8f);
            V.SparkBurst(c, new Color(2.2f, 1.6f, 0.4f), 18, 4f, 0.5f, 0.07f, 0.8f);
            return true;
        }

        /// <summary>Lobisomem chegou: portal de névoa, uivo de poeira e tremor.</summary>
        public static bool InvaderArrives(Vector3 pos)
        {
            if (V == null) return false;
            var c = pos + Vector3.up * 1.2f;
            V.Flash(c, Violet, 4f, 0.3f, 2f);
            V.Ring(pos, Violet, 0.5f, 4.5f, 0.8f, 0.4f, 0.4f);
            V.Ring(pos, Moon, 4.5f, 0.5f, 0.6f, 0.25f, 0.2f);
            V.SmokeBurst(c, MistSmoke, 20, 1f, 2f, 2.5f, 1.8f, 0.6f);
            V.Dust(pos, 16, 1f, 3f, 1.4f, Earth);
            V.Decal(pos, 4f, VfxAssets.Rune, new Color(0.45f, 0.12f, 0.6f, 0.9f), 6f);
            V.AddShake(0.35f, true);
            return true;
        }

        /// <summary>Atordoado: estrelinhas girando sobre a cabeça (chamado todo quadro).</summary>
        public static void StunTick(Vector3 headTop)
        {
            if (V == null || !Every(20f)) return;
            float a = Time.time * 7f + Rnd(0f, 0.3f);
            V.Flares.Emit(headTop + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.45f * Size, new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)) * 2f, 0.35f, 0.25f, new Color(2.2f, 1.9f, 0.6f), -1f, 90f);
        }

        /// <summary>Poeira dos passos (chamado a cada passo pelo UnitFx).</summary>
        public static void Footstep(Vector3 feet, float speed01)
        {
            if (V == null) return;
            V.Smoke.Emit(new Vector3(feet.x, 0.15f, feet.z) + Random.insideUnitSphere * 0.15f, new Vector3(Rnd(-0.3f, 0.3f), Rnd(0.2f, 0.5f), Rnd(-0.3f, 0.3f)), Rnd(0.35f, 0.6f) * (0.6f + speed01 * 0.6f), Rnd(0.5f, 0.8f), new Color(0.3f, 0.25f, 0.2f, 0.45f), -1f, 20f);
        }
    }
}
