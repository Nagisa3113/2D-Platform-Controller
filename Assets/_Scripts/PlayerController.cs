﻿using System;
using UnityEditor;
using UnityEngine;


namespace Myd.Platform.Demo
{
    public struct Input
    {
        public float moveX;
        public float MoveY;
    }
    public class PlayerController : IPlayerContext
    {
        private readonly int GroundMask;

        private readonly Rect normalHitbox = new Rect(0, -0.25f, 0.8f, 1.1f);
        private readonly Rect ductHitBox = new Rect(0, -0.3f, 0.8f, 0.6f);
        Vector2 position;
        Vector2 size;
        Vector2 speed;   //移动向量
        float jumpGraceTimer;
        float varJumpTimer;
        float varJumpSpeed; //
        int moveX;
        private float maxFall;
        private float fastMaxFall;

        private float wallSpeedRetentionTimer; // If you hit a wall, start this timer. If coast is clear within this timer, retain h-speed
        private float wallSpeedRetained;
        private bool onGround;

        private FiniteStateMachine<BaseActionState> stateMachine;

        public PlayerController()
        {
            this.stateMachine = new FiniteStateMachine<BaseActionState>((int)EActionState.Size);
            this.stateMachine.AddState(new NormalState(this));

            this.GroundMask = LayerMask.GetMask("Ground");
        }

        public void Init()
        {
            //根据进入的方式,决定初始状态
            this.stateMachine.SetState((int)EActionState.Normal);
        }

        public void Update(float deltaTime)
        {
            //更新变量状态
            {
                if (speed.y <= 0)
                {
                    //碰撞检测地面
                    this.onGround = CollideY();
                }
                else
                {
                    this.onGround = false;
                }

                //Var Jump
                if (varJumpTimer > 0)
                {
                    varJumpTimer -= deltaTime;
                }

                //撞墙以后的速度保持，Wall Speed Retention
                if (wallSpeedRetentionTimer > 0)
                {
                    if (Math.Sign(speed.x) == -Math.Sign(wallSpeedRetained))
                        wallSpeedRetentionTimer = 0;
                    else if (!CollideCheck(Position + Vector2.right * Math.Sign(wallSpeedRetained) * 0.00001f))
                    {
                        Debug.Log($"====UseWallSpeed:{wallSpeedRetained}");
                        speed.x = wallSpeedRetained;
                        wallSpeedRetentionTimer = 0;
                    }
                    else
                        wallSpeedRetentionTimer -= deltaTime;
                }
            }

            //输入
            this.moveX = Math.Sign(UnityEngine.Input.GetAxisRaw("Horizontal"));

            //落地设置土狼时间
            if (OnGround)
            {
                //dreamJump = false;
                jumpGraceTimer = Constants.JumpGraceTime;
            }
            else
            {
                if (jumpGraceTimer > 0)
                {
                    jumpGraceTimer -= deltaTime;
                }
            }

            //处理逻辑
            stateMachine.Update(deltaTime);

            //更新位置
            UpdatePositionX(speed.x * deltaTime);
            UpdatePositionY(speed.y * deltaTime);
            //Physics
            //if (StateMachine.State != StDreamDash && StateMachine.State != StAttract)
            //    MoveH(Speed.X * Engine.DeltaTime, onCollideH);
            //if (StateMachine.State != StDreamDash && StateMachine.State != StAttract)
            //    MoveV(Speed.Y * Engine.DeltaTime, onCollideV);
        }

        public void Render()
        {
        }

        private void UpdatePositionX(float distX)
        {
            if (distX == 0)
                return;
            //目标位置
            Vector2 direct = Math.Sign(distX) > 0 ? Vector2.right : Vector2.left;
            Vector2 targetPosition = this.position;

            Vector2 origion = this.position + normalHitbox.position + Vector2.up * 0.01f;

            RaycastHit2D hit = Physics2D.BoxCast(origion, normalHitbox.size, 0, direct, Mathf.Abs(distX) + 0.01f, GroundMask);
            if (hit && hit.normal == -direct)
            {
                Debug.Log("================UpdatePositionX:Hit");
                //如果发生碰撞,则移动距离
                targetPosition += direct * (hit.distance - 0.01f);
                //Speed retention
                //if (wallSpeedRetentionTimer <= 0)
                //{
                //    wallSpeedRetained = this.speed.x;
                //    wallSpeedRetentionTimer = Constants.WallSpeedRetentionTime;
                //}
                this.speed.x = 0;
            }
            else
            {
                targetPosition += Vector2.right * distX;
            }
            this.position = targetPosition;
        }
        private void UpdatePositionY(float distY)
        {
            Vector2 targetPosition = this.position;
            Vector2 direct = Math.Sign(distY) > 0 ? Vector2.up : Vector2.down;
            Vector2 origion = this.position + normalHitbox.position;
            RaycastHit2D hit = Physics2D.BoxCast(origion, normalHitbox.size, 0, direct, Mathf.Abs(distY), GroundMask);
            if (hit && hit.normal == -direct)
            {
                //如果发生碰撞,则移动距离
                targetPosition += direct * (hit.distance);
            }
            else
            {
                targetPosition += Vector2.up * distY;
            }
            this.position = targetPosition;
        }

        //针对横向,进行碰撞检测.如果发生碰撞,
        private bool CollideY()
        {
            Vector2 origion = this.position + Vector2.up * normalHitbox.position.y;
            RaycastHit2D hit = Physics2D.BoxCast(origion, normalHitbox.size, 0, Vector2.down, 0.00001f, GroundMask);
            if (hit && hit.normal == Vector2.up)
            {
                return true;
            }
            return false;
        }

        //处理跳跃
        public void Jump()
        {
            this.jumpGraceTimer = 0;
            this.varJumpTimer = Constants.VarJumpTime;
            this.speed.y = Constants.JumpSpeed;
            this.varJumpSpeed = Constants.JumpSpeed;
        }


        #region 实现IPlayerContext接口
        public float WallSpeedRetentionTimer
        {
            get { return this.wallSpeedRetentionTimer; }
            set { this.wallSpeedRetentionTimer = value; }
        }
        public Vector2 Speed
        {
            get
            {
                return this.speed;
            }
            set
            {
                this.speed = value;
            }
        }

        public object Holding => null;

        public bool OnGround => this.onGround;

        public bool JumpPressed => UnityEngine.Input.GetKeyDown(KeyCode.Space);
        public bool JumpChecked => UnityEngine.Input.GetKey(KeyCode.Space);

        public float JumpGraceTimer => jumpGraceTimer;

        public Vector2 Position
        {
            get
            {
                return position;
            }
            set
            {
                this.position = value;
            }
        }

        float IPlayerContext.VarJumpSpeed => this.varJumpSpeed;

        float IPlayerContext.VarJumpTimer
        {
            get
            {
                return this.varJumpTimer;
            }
            set
            {
                this.varJumpTimer = value;
            }
        }


        public int MoveX => moveX;
        public int MoveY => Math.Sign(UnityEngine.Input.GetAxisRaw("Vertical"));

        public float MaxFall { get => maxFall; set => maxFall = value; }
        #endregion

        private bool CollideCheck(Vector2 position)
        {
            return Physics2D.OverlapBox(position, normalHitbox.size, 0, GroundMask);
        }
    }
}