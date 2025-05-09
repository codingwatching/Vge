﻿using Vge.World;
using WinGL.Util;

namespace Vge.Entity.List
{
    /// <summary>
    /// Сущность метательная
    /// </summary>
    public class EntityThrowable : EntityBase
    {
        /// <summary>
        /// Кто метнул предмет
        /// </summary>
        public EntityLiving EntityThrower { get; private set; }
        /// <summary>
        /// Сколько тиков жизни
        /// </summary>
        private int _age = 0;
        /// <summary>
        /// Вес сущности
        /// </summary>
        private readonly float _weight;

        private int _jumpTime;

        public EntityThrowable(EnumEntity type)
        {
            if (type == EnumEntity.Stone)
            {
                Width = .125f;
                Height = .25f;
                _weight = 25;
            }
            else
            {
                Width = .5f;
                Height = 1f;
                _weight = 100;
            }
        }

        /// <summary>
        /// Сущность метательная
        /// </summary>
        /// <param name="entityThrower">Кто метнул</param>
        /// <param name="speedThrower">Скорость метания</param>
        public EntityThrowable(EnumEntity type, CollisionBase collision,
            EntityLiving entityThrower, float speedThrower = .49f)
        {
            EntityThrower = entityThrower;
            Type = type;
            if (type == EnumEntity.Stone)
            {
                Width = .125f;
                Height = .25f;
                _weight = 25;
                Physics = new PhysicsGround(collision, this, .9f);
                speedThrower = .6f;
            }
            else
            {
                Width = .5f;
                Height = 1f;
                _weight = 100;
                Physics = new PhysicsGround(collision, this, 0);
                speedThrower = .4f;
            }

            // с боку
            //PosX = entityThrower.PosX + Glm.Cos(entityThrower.RotationYaw);// * .4f;
            //PosZ = entityThrower.PosZ + Glm.Sin(entityThrower.RotationYaw);// * .4f;
            // спереди
            //PosX = entityThrower.PosX + Glm.Sin(entityThrower.RotationYaw);
            //PosZ = entityThrower.PosZ - Glm.Cos(entityThrower.RotationYaw);
            //PosY = entityThrower.PosY + entityThrower.Eye - .2f;
            // вверх
            PosX = entityThrower.PosX;
            PosZ = entityThrower.PosZ;
            PosY = entityThrower.PosY + entityThrower.Height + .2f;

            //Physics = new PhysicsGround(collision, this, .9f);
            //Physics.SetImpulse(.5f);

            //Physics.Movement.Forward = true;
            //Physics.Movement.Sprinting = true;
            float pitchxz = Glm.Cos(entityThrower.RotationPitch);
            Physics.MotionX = Glm.Sin(entityThrower.RotationYaw) * pitchxz * speedThrower;
            Physics.MotionY = Glm.Sin(entityThrower.RotationPitch) * speedThrower;
            Physics.MotionZ = -Glm.Cos(entityThrower.RotationYaw) * pitchxz * speedThrower;

            //float f1 = rand.NextFloat() * .02f;
            //float f2 = rand.NextFloat() * glm.pi360;
            //motion.x += glm.cos(f2) * f1;
            //motion.z += glm.sin(f2) * f1;
            //Motion = motion;
        }

        /// <summary>
        /// Вес сущности для определения импулса между сущностями,
        /// У кого больше вес тот больше толкает или меньше потдаётся импульсу.
        /// </summary>
        public override float GetWeight() => _weight;

        public override void Update()
        {
            if (Physics != null)
            {
                if (_age > 9000)//1800)
                {
                    SetDead();
                    return;
                }
                _age++;

                if (!Physics.IsPhysicSleep())
                {
                    //Console.Write(PosX);
                    //Console.Write(" ");
                    //Console.Write(PosY);
                    //Console.Write(" ");
                    //Console.WriteLine(PosZ);

                    // Расчитать перемещение в объекте физика
                    Physics.LivingUpdate();

                    if (IsPositionChange())
                    {
                        //float x = -Physics.MotionX;
                        //float z = -Physics.MotionZ;

                        //RotationYaw = Glm.Atan2(z, x) - Glm.Pi90;
                        //RotationPitch = -Glm.Atan2(-Physics.MotionY, Mth.Sqrt(x * x + z * z));
                        //RotationPrevYaw = RotationYaw;
                        //RotationPrevPitch = RotationPitch;

                        PosPrevX = PosX;
                        PosPrevY = PosY;
                        PosPrevZ = PosZ;
                        
                        LevelMotionChange = 2;
                    }
                    else if (Physics.IsPhysicAlmostSleep())
                    {
                        LevelMotionChange = 2;
                    }
                }
                else
                {
                    if (_jumpTime++ > 150)
                    {
                        Physics.MotionY = .5f;
                        _jumpTime = 0;
                        Physics.AwakenPhysics();
                    }
                }
            }
        }
    }
}
