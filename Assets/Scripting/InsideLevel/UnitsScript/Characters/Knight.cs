﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knight : PlayerUnit
{
    #region VARIABLES

    [Header("STATS DE CLASE")]

    [SerializeField]
    public int tilesToPush;

    [Header("MEJORAS DE PERSONAJE")]

    public bool pushFarther;
    public bool pushWider;




    #endregion

   
    public void SetSpecificStats(bool _pushFarther, bool _pushWider)
    {
        //base.SetSpecificStats();
        pushFarther = _pushFarther;
        pushWider = _pushWider;
    }

    //En función de donde este mirando el personaje paso una lista de tiles diferente.

    public override void Attack(UnitBase unitToAttack)
    {
        hasAttacked = true;

       //Este primer if  lo pongo de momento para seguir la misma estructura que con los otros personajes y por si hay que cambiar algo específico como la animación, el sonido...
        if (pushFarther)
        {
            tilesToPush = 2;
            //Animación de ataque
            myAnimator.SetTrigger("Attack");

            //Hago daño
            DoDamage(unitToAttack);

            if (currentFacingDirection == FacingDirection.North)
            {
                unitToAttack.CalculatePushPosition(tilesToPush, myCurrentTile.tilesInLineUp, damageMadeByPush, damageMadeByFall);
            }

            else if (currentFacingDirection == FacingDirection.South)
            {
                unitToAttack.CalculatePushPosition(tilesToPush, myCurrentTile.tilesInLineDown, damageMadeByPush, damageMadeByFall);
            }

            else if (currentFacingDirection == FacingDirection.East)
            {
                unitToAttack.CalculatePushPosition(tilesToPush, myCurrentTile.tilesInLineRight, damageMadeByPush, damageMadeByFall);
            }

            else if (currentFacingDirection == FacingDirection.West)
            {
                unitToAttack.CalculatePushPosition(tilesToPush, myCurrentTile.tilesInLineLeft, damageMadeByPush, damageMadeByFall);
            }

            SoundManager.Instance.PlaySound(AppSounds.KNIGHT_ATTACK);

        }

        else if (pushWider)
        {
            //Animación de ataque
            myAnimator.SetTrigger("Attack");

            //Hago daño
            DoDamage(unitToAttack);

           
               //if (unitToAttack.myCurrentTile.tilesInLineRight.Count > 0 && currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineRight[0].unitOnTile != null)
               //{
               //    DoDamage(currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineRight[0].unitOnTile);
               //}

               //if (unitToAttack.myCurrentTile.tilesInLineLeft.Count > 0 && currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineLeft[0].unitOnTile != null)
               //{
               //    DoDamage(currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineLeft[0].unitOnTile);
               //}
            

               //if (unitToAttack.myCurrentTile.tilesInLineUp.Count > 0 && currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineUp[0].unitOnTile != null)
               //{
               //    DoDamage(currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineUp[0].unitOnTile);
               //}

               //if (unitToAttack.myCurrentTile.tilesInLineDown.Count > 0 && currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineDown[0].unitOnTile != null)
               //{
               //    DoDamage(currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineDown[0].unitOnTile);
               //}

            

            if (currentFacingDirection == FacingDirection.North)
            {              
                unitToAttack.CalculatePushPosition(tilesToPush, myCurrentTile.tilesInLineUp, damageMadeByPush, damageMadeByFall);
            }

            else if (currentFacingDirection == FacingDirection.South)
            {             
                unitToAttack.CalculatePushPosition(tilesToPush, myCurrentTile.tilesInLineDown, damageMadeByPush, damageMadeByFall);
            }

            else if (currentFacingDirection == FacingDirection.East)
            {              
                unitToAttack.CalculatePushPosition(tilesToPush, myCurrentTile.tilesInLineRight, damageMadeByPush, damageMadeByFall);
            }

            else if (currentFacingDirection == FacingDirection.West)
            {
                unitToAttack.CalculatePushPosition(tilesToPush, myCurrentTile.tilesInLineLeft, damageMadeByPush, damageMadeByFall);           
            }

            SoundManager.Instance.PlaySound(AppSounds.KNIGHT_ATTACK);
        }

        else
        {
            //Animación de ataque
            myAnimator.SetTrigger("Attack");

            //Hago daño
            DoDamage(unitToAttack);

            if (currentFacingDirection == FacingDirection.North)
            {
                unitToAttack.CalculatePushPosition(tilesToPush, myCurrentTile.tilesInLineUp, damageMadeByPush, damageMadeByFall);
            }

            else if (currentFacingDirection == FacingDirection.South)
            {
                unitToAttack.CalculatePushPosition(tilesToPush, myCurrentTile.tilesInLineDown, damageMadeByPush, damageMadeByFall);
            }

            else if (currentFacingDirection == FacingDirection.East)
            {
                unitToAttack.CalculatePushPosition(tilesToPush, myCurrentTile.tilesInLineRight, damageMadeByPush, damageMadeByFall);
            }

            else if (currentFacingDirection == FacingDirection.West)
            {
                unitToAttack.CalculatePushPosition(tilesToPush, myCurrentTile.tilesInLineLeft, damageMadeByPush, damageMadeByFall);
            }

            SoundManager.Instance.PlaySound(AppSounds.KNIGHT_ATTACK);
        }
                
        //La base tiene que ir al final para que el bool de hasAttacked se active después del efecto.
        base.Attack(unitToAttack);
    }

    public override void ReceiveDamage(int damageReceived, UnitBase unitAttacker)
    {

        if (unitAttacker != null)
        {
            if (currentFacingDirection == FacingDirection.North && unitAttacker.currentFacingDirection == FacingDirection.South
          || currentFacingDirection == FacingDirection.East && unitAttacker.currentFacingDirection == FacingDirection.West
          || currentFacingDirection == FacingDirection.South && unitAttacker.currentFacingDirection == FacingDirection.North
          || currentFacingDirection == FacingDirection.West && unitAttacker.currentFacingDirection == FacingDirection.East)
            {
                //No recibe daño
                Debug.Log("bloqueado el ataque");
            }

            else
            {
                base.ReceiveDamage(damageReceived, unitAttacker);
            }
        }

        //Si el atacante es null probablemente es un tile de daño o algo por el estilo
        else
        {
            base.ReceiveDamage(damageReceived, unitAttacker);
        }
    }


}
