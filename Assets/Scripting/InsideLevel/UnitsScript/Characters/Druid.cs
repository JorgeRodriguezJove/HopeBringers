﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Druid : PlayerUnit
{
    #region VARIABLES

    [Header("MEJORAS DE PERSONAJE")]

    [SerializeField]
    public  int healedLife;
   
    public int buffHeal;

    [Header("Activas")]
    //ACTIVAS

    //La activa uno depende de cambiar el int healedLife;

    //bool mejora de la activa 1
    public bool individualHealer2;

    //int que indica cuantas unidades de movimiento se mejora a la unidad
    //Jojo, acuerdate de que hay que incremeentar healedLife más aún
    public int movementUpgrade;

    //bool activa 2
    public bool areaHealer;

    //bool mejora activa 2
    public bool areaHealer2;

    [Header("Pasivas")]
    //PASIVAS

    //bool pasiva 1
    public bool tileTransformer;

    [HideInInspector]
    public List<GameObject> tilesSpawned;

    //bool mejora de la pasiva 1
    public bool tileTransformer2;

    //bool pasiva 2
    public bool tileSustitute;

    //bool mejora de la pasiva 2
    public bool tileSustitute2;

    //int que añade bonus al druida si está en un tile de curación
    public int bonusOnTile;

    public GameObject healerTilePref;

    public GameObject shadowHealerTilePref;

    #endregion

    public void SetSpecificStats(int _heal1, bool _heal2,
                                 bool _areaHeal1, bool _areaHeal2,
                                 bool _tile1, bool _tile2,
                                 bool _tileMovement1, bool _tileMovement2)
    {

        //IMPORTANTE REVISAR QUE ESTAN BIEN LOS TEXTOS (NO ESTOY SEGURO DE HABER CORRESPONDIDO CADA MEJORA CON SU TEXTO BIEN)

        activeSkillInfo = AppDruidUpgrades.initialActiveText;
        pasiveSkillInfo = AppDruidUpgrades.initialPasiveText;

        activeTooltipIcon = Resources.Load<Sprite>(AppPaths.PATH_RESOURCE_GENERIC_ICONS + "genericActive");
        pasiveTooltipIcon = Resources.Load<Sprite>(AppPaths.PATH_RESOURCE_GENERIC_ICONS + "genericPasive");

        #region Actives

        healedLife = _heal1;
        individualHealer2 = _heal2;

        areaHealer = _areaHeal1;
        areaHealer2 = _areaHeal2;

        if (areaHealer2)
        {
            activeSkillInfo = AppDruidUpgrades.areaHeal2Text;
            activeTooltipIcon = Resources.Load<Sprite>(AppPaths.PATH_RESOURCE_GENERIC_ICONS + AppDruidUpgrades.areaHeal2);
        }

        else if (areaHealer)
        {
            activeSkillInfo = AppDruidUpgrades.areaHeal1Text;
            activeTooltipIcon = Resources.Load<Sprite>(AppPaths.PATH_RESOURCE_GENERIC_ICONS + AppDruidUpgrades.areaHeal1);
        }

        if (individualHealer2)
        {
            activeSkillInfo = AppDruidUpgrades.heal2Text;
            activeTooltipIcon = Resources.Load<Sprite>(AppPaths.PATH_RESOURCE_GENERIC_ICONS + AppDruidUpgrades.heal2);
        }

        else if (healedLife > 1)
        {
            activeSkillInfo = AppDruidUpgrades.heal1Text;
            activeTooltipIcon = Resources.Load<Sprite>(AppPaths.PATH_RESOURCE_GENERIC_ICONS + AppDruidUpgrades.heal1);
        }

        #endregion

        #region Pasives

        tileTransformer = _tile1;
        tileTransformer2 = _tile2;

        tileSustitute = _tileMovement1;
        tileSustitute2 = _tileMovement2;


        if (tileSustitute2)
        {
            pasiveSkillInfo = AppDruidUpgrades.tileMovement2Text;
            activeTooltipIcon = Resources.Load<Sprite>(AppPaths.PATH_RESOURCE_GENERIC_ICONS + AppDruidUpgrades.tileMovement2);
        }

        else if (tileSustitute)
        {
            pasiveSkillInfo = AppDruidUpgrades.tileMovement1Text;
            activeTooltipIcon = Resources.Load<Sprite>(AppPaths.PATH_RESOURCE_GENERIC_ICONS + AppDruidUpgrades.tileMovement1);
        }

        if (tileTransformer2)
        {
            pasiveSkillInfo = AppDruidUpgrades.tile2Text;
            activeTooltipIcon = Resources.Load<Sprite>(AppPaths.PATH_RESOURCE_GENERIC_ICONS + AppDruidUpgrades.tile2);
        }

        else if (tileTransformer)
        {
            pasiveSkillInfo = AppDruidUpgrades.tile1Text;
            activeTooltipIcon = Resources.Load<Sprite>(AppPaths.PATH_RESOURCE_GENERIC_ICONS + AppDruidUpgrades.tile1);
        }


        #endregion
    }


    public override void Attack(UnitBase unitToAttack)
    {
        hasAttacked = true;

        CheckIfUnitHasMarks(unitToAttack);

        if (areaHealer)
        {
            //Hay que cambiar
            Instantiate(attackParticle, unitToAttack.transform.position, unitToAttack.transform.rotation);

            if (unitToAttack.GetComponent<PlayerUnit>())
            {
                currentHealth -= 1;
                UIM.RefreshTokens();
                UIM.RefreshHealth();

                //COMPROBAR QUE NO DE ERROR EN OTRAS COSAS
                TM.surroundingTiles.Clear();

                TM.GetSurroundingTiles(unitToAttack.myCurrentTile, 1, true, false);
                //Hago daño a las unidades adyacentes
                for (int i = 0; i < TM.surroundingTiles.Count; ++i)
                {
                    if (TM.surroundingTiles[i].unitOnTile != null)
                    {
                        if (areaHealer2)
                        {
                            TM.surroundingTiles[i].unitOnTile.isStunned = false;
                            TM.surroundingTiles[i].unitOnTile.turnStunned = 0;
                            ApplyBuffOrDebuffDamage(TM.surroundingTiles[i].unitOnTile, 0, 0);

                            TM.surroundingTiles[i].unitOnTile.BuffbonusStateDamage = 0;

                        }
                        if (tileTransformer)
                        {
                            Instantiate(healerTilePref, TM.surroundingTiles[i].unitOnTile.transform.position, TM.surroundingTiles[i].unitOnTile.transform.rotation);

                        }
                        TM.surroundingTiles[i].unitOnTile.currentHealth += healedLife;
                    }
                }

                if (tilesSpawned.Count > 0)
                {
                    for (int i = 0; i < tilesSpawned.Count; i++)
                    {
                        Destroy(tilesSpawned[i].gameObject);
                    }

                    tilesSpawned.Clear();
                }
            }
            else
            {
                //Hago daño
                DoDamage(unitToAttack);

                if (currentHealth < maxHealth)
                {
                    currentHealth += healedLife;
                    UIM.RefreshTokens();
                    UIM.RefreshHealth();
                }

                //Hay que cambiar
                SoundManager.Instance.PlaySound(AppSounds.MAGE_ATTACK);

            }
        }

        else
        {
            //Hay que cambiar
            Instantiate(attackParticle, unitToAttack.transform.position, unitToAttack.transform.rotation);

            if (unitToAttack.GetComponent<PlayerUnit>())
            {
                //Hay que cambiar
                SoundManager.Instance.PlaySound(AppSounds.MAGE_ATTACK);
                if (individualHealer2)
                {
                    unitToAttack.movementUds = unitToAttack.GetComponent<PlayerUnit>().fMovementUds + movementUpgrade;
                }
                else if (tileTransformer)
                {
                    Instantiate(healerTilePref, unitToAttack.transform.position, unitToAttack.transform.rotation);

                }

                if (tilesSpawned.Count > 0)
                {
                    for (int i = 0; i < tilesSpawned.Count; i++)
                    {
                        Destroy(tilesSpawned[i].gameObject);
                    }

                    tilesSpawned.Clear();
                }
                

                unitToAttack.currentHealth += healedLife;
                currentHealth -= 1;
                UIM.RefreshTokens();
                UIM.RefreshHealth();
            }
            else
            {
                //Hago daño
                DoDamage(unitToAttack);

                if (currentHealth < maxHealth)
                {
                    currentHealth += healedLife;
                    UIM.RefreshTokens();
                    UIM.RefreshHealth();
                }

                //Hay que cambiar
                SoundManager.Instance.PlaySound(AppSounds.MAGE_ATTACK);

            }
        }
        

        //La base tiene que ir al final para que el bool de hasAttacked se active después del efecto.
        base.Attack(unitToAttack);
        
    }

    protected override void DoDamage(UnitBase unitToDealDamage)
    {

        //Añado este if para el count de honor del samurai
        if (currentFacingDirection == FacingDirection.North && unitToDealDamage.currentFacingDirection == FacingDirection.South
       || currentFacingDirection == FacingDirection.South && unitToDealDamage.currentFacingDirection == FacingDirection.North
       || currentFacingDirection == FacingDirection.East && unitToDealDamage.currentFacingDirection == FacingDirection.West
       || currentFacingDirection == FacingDirection.West && unitToDealDamage.currentFacingDirection == FacingDirection.East
       )
        {
            LM.honorCount++;
        }

        base.DoDamage(unitToDealDamage);
    }

    #region CHECKS
    //AL igual que con el Mago, se hace override a esta función para que pueda atravesar unidades al atacar.
    public override void CheckUnitsAndTilesInRangeToAttack(bool _shouldPaintEnemiesAndShowHealthbar)
    {
        currentUnitsAvailableToAttack.Clear();
        previousTileHeight = 0;

        if (currentFacingDirection == FacingDirection.North)
        {
            if (attackRange <= myCurrentTile.tilesInLineUp.Count)
            {
                rangeVSTilesInLineLimitant = attackRange;
            }
            else
            {
                rangeVSTilesInLineLimitant = myCurrentTile.tilesInLineUp.Count;
            }

            for (int i = 0; i < rangeVSTilesInLineLimitant; i++)
            {
                //Guardo la altura mas alta en esta linea de tiles
                if (myCurrentTile.tilesInLineUp[i].height > previousTileHeight)
                {
                    previousTileHeight = myCurrentTile.tilesInLineUp[i].height;
                }

                //Si hay una unidad
                if (myCurrentTile.tilesInLineUp[i].unitOnTile != null)
                {
                    //Compruebo que la diferencia de altura con mi tile y con el tile anterior es correcto.
                    if (Mathf.Abs(myCurrentTile.tilesInLineUp[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack
                        || Mathf.Abs(myCurrentTile.tilesInLineUp[i].height - previousTileHeight) <= maxHeightDifferenceToAttack)
                    {

                        if (myCurrentTile.tilesInLineUp[i].unitOnTile.currentHealth == myCurrentTile.tilesInLineUp[i].unitOnTile.maxHealth
                            && myCurrentTile.tilesInLineUp[i].unitOnTile.GetComponent<PlayerUnit>())
                        {

                        }
                        else
                        {
                            //Almaceno la primera unidad en la lista de posibles unidades
                            currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineUp[i].unitOnTile);
                        }
                        
                    }

                    else
                    {
                        continue;
                    }
                }

                if (myCurrentTile.tilesInLineUp[i].isEmpty)
                {
                    break;
                }
            }
        }

        if (currentFacingDirection == FacingDirection.South)
        {
            if (attackRange <= myCurrentTile.tilesInLineDown.Count)
            {
                rangeVSTilesInLineLimitant = attackRange;
            }
            else
            {
                rangeVSTilesInLineLimitant = myCurrentTile.tilesInLineDown.Count;
            }

            for (int i = 0; i < rangeVSTilesInLineLimitant; i++)
            {
                //Guardo la altura mas alta en esta linea de tiles
                if (myCurrentTile.tilesInLineDown[i].height > previousTileHeight)
                {
                    previousTileHeight = myCurrentTile.tilesInLineDown[i].height;
                }

                //Si hay una unidad
                if (myCurrentTile.tilesInLineDown[i].unitOnTile != null)
                {
                    //Compruebo que la diferencia de altura con mi tile y con el tile anterior es correcto.
                    if (Mathf.Abs(myCurrentTile.tilesInLineDown[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack
                        || Mathf.Abs(myCurrentTile.tilesInLineDown[i].height - previousTileHeight) <= maxHeightDifferenceToAttack)
                    {
                        if (myCurrentTile.tilesInLineDown[i].unitOnTile.currentHealth == myCurrentTile.tilesInLineDown[i].unitOnTile.maxHealth
                           && myCurrentTile.tilesInLineDown[i].unitOnTile.GetComponent<PlayerUnit>())
                        {

                        }
                        else
                        {
                            //Almaceno la primera unidad en la lista de posibles unidades
                            currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineDown[i].unitOnTile);
                        }
                        
                    }

                    else
                    {
                        continue;
                    }
                }

                if (myCurrentTile.tilesInLineDown[i].isEmpty)
                {
                    break;
                }
            }
        }

        if (currentFacingDirection == FacingDirection.East)
        {
            if (attackRange <= myCurrentTile.tilesInLineRight.Count)
            {
                rangeVSTilesInLineLimitant = attackRange;
            }
            else
            {
                rangeVSTilesInLineLimitant = myCurrentTile.tilesInLineRight.Count;
            }

            for (int i = 0; i < rangeVSTilesInLineLimitant; i++)
            {
                //Guardo la altura mas alta en esta linea de tiles
                if (myCurrentTile.tilesInLineRight[i].height > previousTileHeight)
                {
                    previousTileHeight = myCurrentTile.tilesInLineRight[i].height;
                }

                //Si hay una unidad
                if (myCurrentTile.tilesInLineRight[i].unitOnTile != null)
                {
                    //Compruebo que la diferencia de altura con mi tile y con el tile anterior es correcto.
                    if (Mathf.Abs(myCurrentTile.tilesInLineRight[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack
                        || Mathf.Abs(myCurrentTile.tilesInLineRight[i].height - previousTileHeight) <= maxHeightDifferenceToAttack)
                    {

                        if (myCurrentTile.tilesInLineRight[i].unitOnTile.currentHealth == myCurrentTile.tilesInLineRight[i].unitOnTile.maxHealth
                           && myCurrentTile.tilesInLineRight[i].unitOnTile.GetComponent<PlayerUnit>())
                        {

                        }
                        else
                        {
                            //Almaceno la primera unidad en la lista de posibles unidades
                            currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineRight[i].unitOnTile);
                        }
                       
                    }

                    else
                    {
                        continue;
                    }
                }

                if (myCurrentTile.tilesInLineRight[i].isEmpty)
                {
                    break;
                }
            }
        }

        if (currentFacingDirection == FacingDirection.West)
        {
            if (attackRange <= myCurrentTile.tilesInLineLeft.Count)
            {
                rangeVSTilesInLineLimitant = attackRange;
            }
            else
            {
                rangeVSTilesInLineLimitant = myCurrentTile.tilesInLineLeft.Count;
            }

            for (int i = 0; i < rangeVSTilesInLineLimitant; i++)
            {
                //Guardo la altura mas alta en esta linea de tiles
                if (myCurrentTile.tilesInLineLeft[i].height > previousTileHeight)
                {
                    previousTileHeight = myCurrentTile.tilesInLineLeft[i].height;
                }

                //Si hay una unidad
                if (myCurrentTile.tilesInLineLeft[i].unitOnTile != null)
                {
                    //Compruebo que la diferencia de altura con mi tile y con el tile anterior es correcto.
                    if (Mathf.Abs(myCurrentTile.tilesInLineLeft[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack
                        || Mathf.Abs(myCurrentTile.tilesInLineLeft[i].height - previousTileHeight) <= maxHeightDifferenceToAttack)
                    {


                        if (myCurrentTile.tilesInLineLeft[i].unitOnTile.currentHealth == myCurrentTile.tilesInLineLeft[i].unitOnTile.maxHealth
                           && myCurrentTile.tilesInLineLeft[i].unitOnTile.GetComponent<PlayerUnit>())
                        {

                        }
                        else
                        {
                            //Almaceno la primera unidad en la lista de posibles unidades
                            currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineLeft[i].unitOnTile);
                        }
                       
                    }

                    else
                    {
                        continue;
                    }
                }

                if (myCurrentTile.tilesInLineLeft[i].isEmpty)
                {
                    break;
                }
            }

        }

        if (_shouldPaintEnemiesAndShowHealthbar)
        {
            //Marco las unidades disponibles para atacar de color rojo
            for (int i = 0; i < currentUnitsAvailableToAttack.Count; i++)
            {
                CalculateDamage(currentUnitsAvailableToAttack[i]);
                if (currentUnitsAvailableToAttack[i].GetComponent<PlayerUnit>())
                {

                    currentUnitsAvailableToAttack[i].canvasUnit.SetActive(true);
                    currentUnitsAvailableToAttack[i].canvasUnit.GetComponent<CanvasHover>().damageNumber.SetText("+" + healedLife.ToString());
                    currentUnitsAvailableToAttack[i].previsualizeAttackIcon.SetActive(true);
                }

                else
                {
                    currentUnitsAvailableToAttack[i].ColorAvailableToBeAttackedAndNumberDamage(damageWithMultipliersApplied);
                }

                currentUnitsAvailableToAttack[i].HealthBarOn_Off(true);
            }
        }
    }
    #endregion

    public override void ShowAttackEffect(UnitBase _unitToAttack)
    {

        if (areaHealer)
        {
            if (_unitToAttack.GetComponent<PlayerUnit>())
            {
                TM.surroundingTiles.Clear();

                TM.GetSurroundingTiles(_unitToAttack.myCurrentTile, 1, true, false);

                for (int i = 0; i < TM.surroundingTiles.Count; ++i)
                {
                    if (TM.surroundingTiles[i] != null)
                    {
                        tilesInEnemyHover.Add(TM.surroundingTiles[i]);

                    }
                }

                for (int i = 0; i < tilesInEnemyHover.Count; i++)
                {
                    tilesInEnemyHover[i].ColorHeal();

                    if (tileTransformer)
                    {
                        GameObject shadowTile = Instantiate(shadowHealerTilePref, tilesInEnemyHover[i].transform.position, tilesInEnemyHover[i].transform.rotation);
                        tilesSpawned.Add(shadowTile);
                    }

                    if (tilesInEnemyHover[i].unitOnTile != null)
                    {
                        tilesInEnemyHover[i].unitOnTile.ColorAvailableToBeHealed();
                    }
                }
            }
            else
            {
                _unitToAttack.ColorAvailableToBeAttackedAndNumberDamage(-1);
                _unitToAttack.myCurrentTile.ColorAttack();
            }
        }
        else
        {

            if (_unitToAttack != null && _unitToAttack.GetComponent<PlayerUnit>())
            {
                if (tileTransformer)
                {
                    GameObject shadowTile = Instantiate(shadowHealerTilePref, _unitToAttack.transform.position, _unitToAttack.transform.rotation);
                    tilesSpawned.Add(shadowTile);
                }
              
                _unitToAttack.ColorAvailableToBeHealed();
                _unitToAttack.myCurrentTile.ColorHeal();
            }
            else if (_unitToAttack != null)
            {
                _unitToAttack.ColorAvailableToBeAttackedAndNumberDamage(-1);
                _unitToAttack.myCurrentTile.ColorAttack();

            }

        }

    }

    public override void HideAttackEffect(UnitBase _unitToAttack)
    {
        if (tileTransformer)
        {
            if (tilesSpawned.Count > 0)
            {
                for (int i = 0; i < tilesSpawned.Count; i++)
                {
                    Destroy(tilesSpawned[i].gameObject);
                }

                tilesSpawned.Clear();
            }
        }

        for (int i = 0; i < tilesInEnemyHover.Count; i++)
        {
            tilesInEnemyHover[i].ColorDesAttack();

            if (tilesInEnemyHover[i].unitOnTile != null)
            {
                tilesInEnemyHover[i].unitOnTile.ResetColor();
            }
        }
        tilesInEnemyHover.Clear();

        _unitToAttack.ResetColor();
        _unitToAttack.myCurrentTile.ColorDesAttack();
    }
}
