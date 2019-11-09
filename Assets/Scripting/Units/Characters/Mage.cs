﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mage : PlayerUnit
{
    #region VARIABLES

    [Header("SPECIAL VARIABLES FOR CHARACTER")]

    [SerializeField]
    protected GameObject ChargingParticle;

    #endregion

    //En función de donde este mirando el personaje paso una lista de tiles diferente.
    public override void Attack(UnitBase unitToAttack)
    {
        //Hago daño
        CalculateDamage(unitToAttack);
        DoDamage(unitToAttack);

        SoundManager.Instance.PlaySound(AppSounds.MAGE_ATTACK);

        //La base tiene que ir al final para que el bool de hasAttacked se active después del efecto.
        base.Attack(unitToAttack);
    }
}
