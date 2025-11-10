using NUnit.Framework;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public int currentHealth;
    public int remainingMovement;
    public bool hasAttacked;
    public bool hasMovedThisTurn;
    public int OwnerPlayerID;
    public HexCell CurrentCell;
}
