﻿//#define TPS20
using System;
using System.Collections.Generic;
using Vge.Entity.Sizes;
using Vge.Util;
using Vge.World;
using WinGL.Util;

namespace Vge.Entity.Physics
{
    /// <summary>
    /// Физика по земле
    /// </summary>
    public class PhysicsGround : PhysicsBase
    {
#if TPS20
        // TPS 20 
          
        /// <summary>
        /// Параметр падения
        /// </summary>
        public const float Gravity = .08f;
        /// <summary>
        /// Сопротивление воздуха
        /// </summary>
        private const float _airDrag = .98f;
        /// <summary>
        /// Ускорение в воздухе
        /// </summary>
        private const float _airborneAcceleration = .02f;
        /// <summary>
        /// Скорость
        /// </summary>
        private const float _speed = .1f;
        /// <summary>
        /// Ускорение при прыжке в высоту
        /// </summary>
        private const float _airborneJumpInHeight = .42f;
        /// <summary>
        /// Повторный прыжок через количество тиков
        /// </summary>
        private const byte _reJump = 10;

#else

        // 30 TPS
        /// <summary>
        /// Параметр падения
        /// </summary>
        public const float Gravity = .039f;
        /// <summary>
        /// Сопротивление воздуха
        /// </summary>
        public const float AirDrag = .9869f;
        /// <summary>
        /// Ускорение в воздухе
        /// </summary>
        protected const float _airborneAcceleration = .01333f;
        /// <summary>
        /// Скорость
        /// </summary>
        protected const float _speed = .0667f;

#endif
        /// <summary>
        /// Отскок от гравитации горизонта
        /// </summary>
        public const float GravityRebound = Gravity * 2f;

        /// <summary>
        /// Скользкость по умолчанию
        /// </summary>
        private const float _defaultSlipperiness = .6f;
        
        
        /// <summary>
        /// Не прыгаем (момент взлёта)
        /// </summary>
        private bool _isNotJump;
        /// <summary>
        /// Высота авто прыжка
        /// </summary>
        private float _heightAutoJump;
        /// <summary>
        /// Имеется ли авто прыжок
        /// </summary>
        private bool _isAutoJump;

        /// <summary>
        /// Инерция в воздухе
        /// </summary>
        private readonly float _airborneInertia;

        /// <summary>
        /// Физика для сущности которая имеет силу для перемещения
        /// </summary>
        protected PhysicsGround(CollisionBase collision, EntityBase entity) 
            : base(collision, entity) => _airborneInertia = .91f;

        /// <summary>
        /// Физика для предмета которые не имеет силы для перемещения но может имет отскок от предметов
        /// </summary>
        /// <param name="rebound">Коэффициент отскока, 0 нет отскока, 1 максимальный</param>
        public PhysicsGround(CollisionBase collision, EntityBase entity, float rebound)
            : base(collision, entity, rebound) => _airborneInertia = AirDrag;

        /// <summary>
        /// Задать высоту автопрыжка, если 0 нет авто прыжка
        /// </summary>
        public PhysicsGround SetHeightAutoJump(float height)
        {
            _heightAutoJump = height;
            _isAutoJump = height != 0;
            return this;
        }

        /// <summary>
        /// Обновить данные в такте игры
        /// </summary>
        public override void LivingUpdate()
        {
            // Если имеется сила для движения, тогда проверяем наличие прыжка
            _LivingUpdateJump();

            // Лимит по максимальному импульсу
            _ImpulseLimit();

            // Ускорение
            float acceleration;
            // Параметр инерции
            float inertia;
            // Трение с блоком
            if (Entity.OnGround)
            {
                // трение блока под ногами
                inertia = _airborneInertia * _defaultSlipperiness; // блок под ногами

                // корректировка скорости, с трением
                //friction = GetAIMoveSpeed(strafe, forward) * param;

                // Если имеется сила для движения, тогда корректируем наличие скорости
                float speed = _LivingUpdateSpeed();

                // Ускорение
                acceleration = speed * 0.16277136f / (inertia * inertia * inertia);
            }
            else
            {
                // трение блока в воздухе
                inertia = _airborneInertia;
                // Ускорение
                acceleration = _LivingUpdateSprinting();
            }
            // Если имеется сила для движения, задаём вектор передвижения
            _LivingUpdateMotion(acceleration);

            // Проверка каллизии
            if (Entity.Size is ISizeEntityBox sizeEntityBox)
            {
                _CheckMoveColliding(sizeEntityBox);
            }
            else
            {
                _CheckMoveCollidingPoint();
            }

            // Если мелочь убираем
            _ResetMinimumMotion();

            // Фиксируем перемещение
            IsMotionChange = MotionX != 0 || MotionY != 0 || MotionZ != 0;

            if (IsMotionChange)
            {
                Entity.PosX += MotionX;
                Entity.PosY += MotionY;
                Entity.PosZ += MotionZ;
                MotionHorizon = Glm.Distance(new Vector2(MotionX, MotionZ));
                MotionVertical = Mth.Abs(MotionY);
                AwakenPhysics();

                //Debug.Player = Entity.GetChunkPosition();

                //if (Entity.Type == EnumEntity.Stone)
                //{
                //    System.Console.Write("Y:");
                //    System.Console.Write(Entity.PosY);
                //    System.Console.Write(" X:");
                //    System.Console.Write(Entity.PosX);
                //    System.Console.Write(" MY:");
                //    System.Console.Write(MotionY);
                //    System.Console.Write(" MX:");
                //    System.Console.WriteLine(MotionX);
                //}
            }
            else
            {
                MotionHorizon = MotionVertical = 0;
                if (_indexSleep > 0) _indexSleep--;
            }
            // Параметр падение 
            MotionY -= Gravity; // minecraft .08f

            // Инерция
            MotionX *= inertia;
            MotionY *= AirDrag;
            MotionZ *= inertia;
        }

        /// <summary>
        /// Фиксация возможен ли авто прыжок
        /// </summary>
        protected override void _AutoNotJump(float y)
        {
            if (_isAutoJump)
            {
                // Не прыгаем (момент взлёта)
                _isNotJump = Entity.OnGround || MotionY != y && MotionY < 0f;
            }
        }
        /// <summary>
        /// Авто прыжок
        /// </summary>
        protected override void _AutoJump(AxisAlignedBB boundingBox, ref float x, ref float y, ref float z)
        {
            // Запуск проверки авто прыжка
            if (_isAutoJump && _isNotJump && (MotionX != x || MotionZ != z))
            {
                // Кэш для откада, если авто прыжок не допустим
                float monCacheX = x;
                float monCacheY = y;
                float monCacheZ = z;

                float heightAutoJump = _heightAutoJump;
                // Если сидим авто прыжок в двое ниже
                if (_IsMovementSneak())
                {
                    heightAutoJump *= 0.5f;
                }

                y = heightAutoJump;
                Collision.StaticBoundingBoxes(boundingBox.AddCoordBias(MotionX, y, MotionZ));
                ListFast<AxisAlignedBB> aabbs = Collision.ListBlock;
                int count = aabbs.Count;
                AxisAlignedBB aabbEntity2 = boundingBox.Clone();
                AxisAlignedBB aabb = aabbEntity2.AddCoordBias(MotionX, 0, MotionZ);

                // Находим смещение по Y
                float y2 = y;
                for (int i = 0; i < count; i++) y2 = aabbs[i].CalculateYOffset(aabb, y2);
                aabbEntity2 = aabbEntity2.Offset(0, y2, 0);

                // Находим смещение по X
                float x2 = MotionX;
                for (int i = 0; i < count; i++) x2 = aabbs[i].CalculateXOffset(aabbEntity2, x2);
                aabbEntity2 = aabbEntity2.Offset(x2, 0, 0);

                // Находим смещение по Z
                float z2 = MotionZ;
                for (int i = 0; i < count; i++) z2 = aabbs[i].CalculateZOffset(aabbEntity2, z2);
                aabbEntity2 = aabbEntity2.Offset(0, 0, z2);

                AxisAlignedBB aabbEntity3 = boundingBox.Clone();

                // Находим смещение по Y
                float y3 = y;
                for (int i = 0; i < count; i++) y3 = aabbs[i].CalculateYOffset(aabbEntity3, y3);
                aabbEntity3 = aabbEntity3.Offset(0, y3, 0);

                // Находим смещение по X
                float x3 = MotionX;
                for (int i = 0; i < count; i++) x3 = aabbs[i].CalculateXOffset(aabbEntity3, x3);
                aabbEntity3 = aabbEntity3.Offset(x3, 0, 0);

                // Находим смещение по Z
                float z3 = MotionZ;
                for (int i = 0; i < count; i++) z3 = aabbs[i].CalculateZOffset(aabbEntity3, z3);
                aabbEntity3 = aabbEntity3.Offset(0, 0, z3);

                y = -heightAutoJump;

                if (x2 * x2 + z2 * z2 > x3 * x3 + z3 * z3)
                {
                    x = x2;
                    z = z2;
                    // Находим итоговое смещение по Y
                    for (int i = 0; i < aabbs.Count; i++) y = aabbs[i].CalculateYOffset(aabbEntity2, y);
                }
                else
                {
                    x = x3;
                    z = z3;
                    // Находим итоговое смещение по Y
                    for (int i = 0; i < aabbs.Count; i++) y = aabbs[i].CalculateYOffset(aabbEntity3, y);
                }

                Collision.ListBlock.ClearFull();

                if (monCacheX * monCacheX + monCacheZ * monCacheZ >= x * x + z * z)
                {
                    // Нет авто прыжка, откатываем значение обратно
                    x = monCacheX;
                    y = monCacheY;
                    z = monCacheZ;
                }
                else
                {
                    // Авто прыжок
                    Entity.PosY += y + heightAutoJump;
                    y = 0;
                }
            }
        }

        #region Living

        /// <summary>
        /// Проверяем наличие прыжка для живой сущности
        /// </summary>
        protected virtual void _LivingUpdateJump() { }

        /// <summary>
        /// Определяем и передаём скорость перемещения для живой сущности
        /// </summary>
        protected virtual float _LivingUpdateSpeed() => _speed;

        /// <summary>
        /// Проверяем наличие ускорения для живой сущности, возвращает скорость
        /// </summary>
        protected virtual float _LivingUpdateSprinting() => _airborneAcceleration;

        /// <summary>
        /// Если имеется задаём вектор передвижени для живой сущности
        /// </summary>
        protected virtual void _LivingUpdateMotion(float acceleration) { }

        /// <summary>
        /// Сидит ли сущность
        /// </summary>
        protected virtual bool _IsMovementSneak() => false;

        #endregion
    }
}
