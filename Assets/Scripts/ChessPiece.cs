using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Team {
    Black,
    White
}

public class ChessPiece : MonoBehaviour
{
    public Team team;

    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Animator animator;

    private void Start()
    {
        if (animator != null)
        {
            animator.enabled = false;
        }
    }

    void Update()
    {
        if (team == Team.White)
        {
            sprite.color = Color.white;
        }
        else
        {
            sprite.color = Color.black;
        }
    }

    public void SwitchTeam(Team nextTeam)
    {
        animator.enabled = true;
        team = nextTeam;
        animator.Play(team == Team.White ? "Black To White" : "White To Black");
    }
}
