using UnityEngine;


[System.Serializable]
public class HardcodedUnitStats
{
   public string unitName;
    public UnitType unitType;
    public int cost;
    public int maxHealth;
    public int attackPower;
    public int attackRange;
    public int movementPoints;

    public HardcodedUnitStats(string name, UnitType type, int cost, int hp, int attack, int range, int movement)
    {
        this.unitName = name;
        this.unitType = type;
        this.cost = cost;
        this.maxHealth = hp;
        this.attackPower = attack;
        this.attackRange = range;
        this.movementPoints = movement;
    }

    public static HardcodedUnitStats Infantry => new HardcodedUnitStats(
        "Infantry", UnitType.Infantry,
        cost: 20,
        hp: 20,
        attack: 10,
        range: 1,
        movement: 4
    );

    public static HardcodedUnitStats Cavalry => new HardcodedUnitStats(
        "Cavalry", UnitType.Cavalry,
        cost: 30,
        hp: 30,
        attack: 12,
        range: 1,
        movement: 6
    );

    public static HardcodedUnitStats Artillery => new HardcodedUnitStats(
        "Artillery", UnitType.Artillery,
        cost: 40,
        hp: 40,
        attack: 15,
        range: 2,
        movement: 3
    );

}
