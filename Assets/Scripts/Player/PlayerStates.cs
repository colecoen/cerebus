using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerStates
{
    public bool grounded;
    public bool walking;
    public bool running;
    public bool jumping;
    public bool falling;
    public bool facingRight;

    public PlayerStates()
    {
        this.facingRight = false;
        this.ResetStates();
    }

    public void ResetStates()
    {
        grounded = false;
        walking = false;
        running = false;
        jumping = false;
        falling = false;
        facingRight = false;
    }
}