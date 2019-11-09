﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class UnitBase : MonoBehaviour
{
    #region VARIABLES

    [Header("STATS")]

    //Variable que se usará para ordenar a las unidades
    [SerializeField]
    public int speed;

    //Vida máxima que tiene cada unidad
    [SerializeField]
    public int maxHealth;

    //Vida actual de la unidad.
    [HideInInspector]
    public int currentHealth;

    //Uds movimiento máximas de la unidad.
    [SerializeField]
    public int movementUds;

    [Header("DAMAGE")]

    //Daño de la unidad
    [SerializeField]
    protected int baseDamage;

    //Daño cuándo ataca por la espalda
    [SerializeField]
    protected float multiplicatorBackAttack;

    //Daño cuándo ataca por la espalda
    [SerializeField]
    protected float multiplicatorMoreHeight;

    //Daño cuándo ataca por la espalda
    [SerializeField]
    protected float multiplicatorLessHeight;

    //Rango del ataque (en general será 1 a no ser que ataquen a distancia).
    [SerializeField]
    protected int range;

    [Header("LOGIC")]

    //Tile en el que está el personaje actualmente. Se setea desde el editor.
    public IndividualTiles myCurrentTile;

    //Enum con las cuatro posibles direcciones en las que puede estar mirando una unidad.
    [HideInInspector]
    public enum FacingDirection { North, East, South, West }

    //Dirección actual. ESTÁ EN SERIALIZEFIELD PARA PROBARLO.
    [SerializeField]
    public FacingDirection currentFacingDirection;

    //Posición a la que tiene que moverse la unidad actualmente
    protected Vector3 currentTileVectorToMove;

    //De momento se guarda aquí pero se podría contemplar que cada personaje tuviese un tiempo distinto.
    [SerializeField]
    protected float timePushAnimation;

    //De momento se guarda aquí pero se podría contemplar que cada personaje tuviese un tiempo distinto.
    [SerializeField]
    protected float timeMovementAnimation;

    //Tiempo que tarda en rotar a la unidad.
    [SerializeField]
    protected float timeDurationRotation;

    //Modelo de la unidad
    [SerializeField]
    protected GameObject unitModel;

    [Header("ATAQUE")]

    //Variable en la que guardo el daño a realizar
    protected float damageWithMultipliersApplied;

    [SerializeField]
    protected float maxHeightDifferenceToAttack;

    //Lista de posibles unidades a las que atacar
    [HideInInspector]
    public List<UnitBase> currentUnitsAvailableToAttack;

    //Variable que guarda el número más pequeño al comparar el rango del personaje con el número de tiles disponibles para atacar.
    protected int rangeVSTilesInLineLimitant;

    [Header("STATS GENÉRICOS")]

    //Daño que hace cada unidad por choque
    [SerializeField]
    protected int damageMadeByPush;

    //Daño que hace cada unidad por choque
    [SerializeField]
    protected int damageMadeByFall;

    [Header("FEEDBACK")]

    //Material inicial y al ser seleccionado
    protected Material initMaterial;
    [SerializeField]
    private Material AvailableToBeAttackedColor;

	[SerializeField]
	private GameObject healthBar;

	[SerializeField]
	[@TextAreaAttribute(15, 20)]
	public string unitInfo;

	//Este canvas sirve para mostrar temas de vida al hacer hover en el caso del enemigo y en el caso del player (no está implementado) sirve para mostrar barra de vida.
	[SerializeField]
    private GameObject canvasUnit;

	[Header("TEXT")]

	//Texto que describe a la unidad.
	[SerializeField]
	public string characterDescription;

	//Icono que aparece en la lista de turnos.
	[SerializeField]
	public Sprite unitIcon;

	//Canvas que muestra la vida de la unidad
	[SerializeField]
	protected Canvas myCanvasHealthbar;

	#endregion

	#region DAMAGE_&_DIE

	//Calcula PERO NO aplico el daño a la unidad elegida
	protected void CalculateDamage(UnitBase unitToDealDamage)
    {
        //Reseteo la variable de daño a realizar
        damageWithMultipliersApplied = baseDamage;

        //Si estoy en desventaja de altura hago menos daño
        if (unitToDealDamage.myCurrentTile.height > myCurrentTile.height)
        {
            damageWithMultipliersApplied *= multiplicatorLessHeight;
        }

        //Si estoy en ventaja de altura hago más daño
        else if (unitToDealDamage.myCurrentTile.height < myCurrentTile.height)
        {
            damageWithMultipliersApplied *= multiplicatorMoreHeight;
        }

        //Si le ataco por la espalda hago más daño
        if (unitToDealDamage.currentFacingDirection == currentFacingDirection)
        {
            //Ataque por la espalda
            damageWithMultipliersApplied *= multiplicatorBackAttack;
        }
    }

    //Aplico el daño a la unidad elegida
    protected void DoDamage(UnitBase unitToDealDamage)
    {
        CalculateDamage(unitToDealDamage);
        //Una vez aplicados los multiplicadores efectuo el daño.
        unitToDealDamage.ReceiveDamage(Mathf.RoundToInt(damageWithMultipliersApplied), this);
    }

    //Función para recibir daño
    public virtual void ReceiveDamage(int damageReceived, UnitBase unitAttacker)
    {
        //Cada unidad se resta vida con esta función.
        //Lo pongo en unit base para que sea genérico entre unidades y no tener que hacer la comprobación todo el rato.
    }

    public virtual void Die()
    {
        //Cada unidad hace lo propio al morir
    }

    #endregion

    #region PUSH
    //Función genérica que sirve para calcular a que tile debe ser empujada una unidad
    //La función pide tatno el daño pro caída como el daño de empujón de la unidad atacante ya que pueden existir mejoras que modifiquen estos valores.
    public void CalculatePushPosition(int numberOfTilesMoved, List<IndividualTiles> tilesToCheckForCollision, int attackersDamageByPush, int attackersDamageByFall)
    {
        Debug.Log("Empuje");

        //Si no hay tiles en la lista me han empujado contra un borde
        //Tiene que ser menor o igual que 1 en vez de 0 porque para empujar a una unidad contra el borde, la unidad que empuja siempre va a necesitar 1 tile para atacar (que es donde está la unidad a la que voy a atacar)
        if (tilesToCheckForCollision.Count <= 1)
        {
            Debug.Log("borde");

            //Recibo daño 
            ReceiveDamage(attackersDamageByPush, null);

            //Hago animación de rebote??
        }

        //Si hay tiles en la lista me empjuan contra tiles que no son bordes 
        else
        {
            for (int i = 1; i <= numberOfTilesMoved; i++)
            {
                //El tile al que empujo está más alto (pared)
                if (tilesToCheckForCollision[i].height > myCurrentTile.height)
                {
                    Debug.Log("pared");
                    //Recibo daño 
                    ReceiveDamage(attackersDamageByPush, null);

                    //Desplazo a la unidad
                    MoveToTilePushed(tilesToCheckForCollision[i - 1]);

                    //Animación de rebote??

                    return;
                }

                //El tile al que empujo está más bajo (caída)
                else if (Mathf.Abs(tilesToCheckForCollision[i].height - myCurrentTile.height) > 1)
                {
                    Debug.Log("caída");

                    //Compruebo la altura de la que lo tiro??

                    //Compruebo si hay otra unidad
                    if (tilesToCheckForCollision[i].unitOnTile != null)
                    {
                        ReceiveDamage(attackersDamageByFall, null);
                        tilesToCheckForCollision[i].unitOnTile.ReceiveDamage(attackersDamageByPush, null);

                        if (tilesToCheckForCollision[i].unitOnTile.currentHealth > currentHealth)
                        {
                            //Muere la unidad que cae
                            Die();
                        }

                        else
                        {
                            //Muere la unidad de abajo
                            tilesToCheckForCollision[i].unitOnTile.Die();
                        }
                    }

                    else
                    {
                        ReceiveDamage(attackersDamageByFall, null);
                    }

                    //Que pasa si hay un obstáculo en el tile de abajo?

                    MoveToTilePushed(tilesToCheckForCollision[i]);

                    return;
                }

                //Si la altura del tile al que empujo y la mía son iguales compruebo si el tile está vacío, es un obstáculo o tiene una unidad.
                else
                {
                    //Es tile vacío u obstáculo
                    if (tilesToCheckForCollision[i].isEmpty || tilesToCheckForCollision[i].isObstacle)
                    {
                        Debug.Log("vacío");
                        //Recibo daño 
                        ReceiveDamage(attackersDamageByPush, null);

                        // Desplazo a la unidad
                        MoveToTilePushed(tilesToCheckForCollision[i - 1]);

                        //Animación de rebote??

                        return;
                    }

                    //Es tile con unidad
                    else if (tilesToCheckForCollision[i].unitOnTile != null)
                    {
                        Debug.Log("otra unidad");
                        //Recibo daño 
                        ReceiveDamage(attackersDamageByPush, null);

                        //Hago daño a la otra unidad
                        tilesToCheckForCollision[i].unitOnTile.ReceiveDamage(attackersDamageByPush, null);

                        //Desplazo a la unidad
                        MoveToTilePushed(tilesToCheckForCollision[i-1]);

                        //Animación de rebote??

                        return;
                    }
                }
            }

            //Si sale del for entonces es que todos los tiles que tiene que comprobar son normales y simplemente lo muevo al último tile

            SoundManager.Instance.PlaySound(AppSounds.COLLISION);

            //Desplazo a la unidad
            MoveToTilePushed(tilesToCheckForCollision[numberOfTilesMoved]);
        }
    }

    //Función que ejecuta el movimiento del push
    private void MoveToTilePushed(IndividualTiles newTile)
    {
        //Mover al nuevo tile
        currentTileVectorToMove = new Vector3(newTile.tileX, newTile.height + 1, newTile.tileZ);
        transform.DOMove(currentTileVectorToMove, timePushAnimation);

        //Aviso a los tiles del cambio de posición
        myCurrentTile.unitOnTile = null;
        myCurrentTile = newTile;
        myCurrentTile.unitOnTile = this;
    }

    #endregion

    #region COLORS

    //Cambiar a color que indica que puede ser atacado
    public void ColorInitial()
    {
        unitModel.GetComponent<MeshRenderer>().material = initMaterial;
    }

    //Cambiar a color que indica que puede ser atacado
    public void ColorAvailableToBeAttacked()
    {
        unitModel.GetComponent<MeshRenderer>().material = AvailableToBeAttackedColor;
    }

    #endregion

    #region UI_HOVER

    public void EnableCanvasHover(int damageReceived)
    {
        canvasUnit.SetActive(true);
        canvasUnit.GetComponent<CanvasHover>().damageNumber.SetText(damageReceived.ToString());
    }

    public void DisableCanvasHover()
    {
        canvasUnit.SetActive(false);
    }

	public void HealthBarOn_Off(bool isOn)
	{
		healthBar.SetActive(isOn);
	}

	#endregion

}
