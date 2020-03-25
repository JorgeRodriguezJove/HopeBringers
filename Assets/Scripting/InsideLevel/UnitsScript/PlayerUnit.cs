﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class PlayerUnit : UnitBase
{
    #region VARIABLES

    [Header("COLOCACIÓN DE UNIDAD")]
    [HideInInspector]
    public Transform initialPosInBox;

    [Header("LOGICA PLAYER")]

    //Bools que indican si el personaje se ha movido y si ha atacado.
    [HideInInspector]
    public bool hasMoved = false;
    [HideInInspector]
    public bool hasAttacked = false;
    [HideInInspector]
    public bool isMovingorRotating = false;

    //Bool para saber si puedo hacer hover a las unidades 
    [HideInInspector]
    public bool canHover;

    //Lista de posibles unidades a las que atacar
    [HideInInspector]
    public List<UnitBase> currentUnitsAvailableToAttack;

    //Lista de posibles unidades a las que atacar
    [HideInInspector]
    public List<IndividualTiles> currentTilesInRangeForAttack;

    //Solo lo uso para el Rogue
    [HideInInspector]
    public bool hasUsedExtraTurn;

    [Header("MOVIMIENTO")]

    //Camino que tiene que seguir la unidad para moverse
    protected List<IndividualTiles> myCurrentPath = new List<IndividualTiles>();

    //Tiempo a esperar tras atacar
    [SerializeField]
    protected float timeWaitAfterAttack;

	[Header("INFO")]
	[@TextAreaAttribute(15, 20)]
	public string activeSkillInfo;
	[SerializeField]
	public Sprite attackTooltipImage;
	[@TextAreaAttribute(15, 20)]
	public string pasiveSkillInfo;
	[SerializeField]
	public Sprite pasiveTooltipImage;
	public string attackInfo;

	[Header("FEEDBACK")]

    [SerializeField]
    public Material selectedMaterial;

    [SerializeField]
    private Material finishedMaterial;

	[SerializeField]
    public Canvas canvasWithRotationArrows;

    //Flecha que indica al jugador si la unidad aún pueda realizar acciones.
    [SerializeField]
    private GameObject arrowIndicator;

    [SerializeField]
    protected GameObject movementTokenInGame;

    [SerializeField]
    private GameObject attackTokenInGame;

    [HideInInspector]
    public GameObject myPanelPortrait;
    [SerializeField]
	public Sprite portraitImage;

	[SerializeField]
	public GameObject actionAvaliablePanel;
	

	//Para el tooltip de ataque

	[Header("REFERENCIAS")]

    [HideInInspector]
    public LevelManager LM;
    [HideInInspector]
    public UIManager UIM;

    //Añado esto para que el mage pueda acceder a la función de GetSurroundingTiles()
    public TileManager TM;

    //Este int lo pongo para saber el primer número y que así el tier 2 del Watcher no esté restando a los current movementsUds
    public int fMovementUds;

    #endregion

    #region INIT

    private void Awake()
    {
        //Referencia al LM y me incluyo en la lista de personajes del jugador
        LM = FindObjectOfType<LevelManager>();

        TM = FindObjectOfType<TileManager>();

		//Referencia al UIM 
		UIM = FindObjectOfType<UIManager>();

        //Inicializo componente animator, material inicial y particula mov
        myAnimator = GetComponent<Animator>();
        initMaterial = unitMaterialModel.GetComponent<SkinnedMeshRenderer>().material;
        movementParticle.SetActive(false);

        fMovementUds = movementUds;


    //if (LM.FuncionarSinHaberSeleccionadoPersonajesEnEscenaMapa)
    //{
    //    currentHealth = maxHealth;
    //}

    //Vigilar esto. He comentado lo anterior y puesto esto porque si no al colocar los personajes si se colocaban al principio del nivel  tenian 0 de vida.
    currentHealth = maxHealth;
    }

    //Stats genéricos que tienen todos los personajes.
    //Los stats especificos se ponen en cada personaje
    public void SetMyGenericStats(int _maxHealth, int _baseDamage, 
                                  int _attackRange)
    {
        maxHealth = _maxHealth;

        //Comprobar que esto no hace que el baseDamage se ponga a 0
        baseDamage = _baseDamage;

        attackRange = _attackRange;





        //Una vez seteadas todas las variables, inicializo mi vida actual
        currentHealth = maxHealth;
    }

    #endregion

    #region TURN_STATE

    //Reseteo las variables
    public void ResetUnitState()
    {

        //Compruebo si los tiles de daño tienen que hacer daño. 
        for (int i = 0; i < LM.damageTilesInBoard.Count; i++)
        {
            if (LM.damageTilesInBoard[i].unitToDoDamage != null 
                && LM.damageTilesInBoard[i].unitToDoDamage.GetComponent<PlayerUnit>() != null 
                && LM.damageTilesInBoard[i].unitToDoDamage.GetComponent<PlayerUnit>() == this)
            {
                
                LM.damageTilesInBoard[i].CheckHasToDoDamage();
                LM.damageTilesInBoard[i].damageDone = true;
                break;
            }
        }
        if (!isDead)
        {

            //Añado esto para stunnear a los enemigos 
            if (!isStunned)
            {
                turnsWithBuffOrDebuff--;
                if (turnsWithBuffOrDebuff <= 0)
                {
                    BuffbonusStateDamage = 0;
                }

                if (arrowIndicator != null)
                {
                    arrowIndicator.SetActive(true);
                }
                hasMoved = false;
                if (movementTokenInGame != null)
                {
                    movementTokenInGame.SetActive(true);
                }
                hasAttacked = false;
                if (attackTokenInGame != null)
                {
                    attackTokenInGame.SetActive(true);
                }

                //Refresco de los tokens para resetearlos en pantalla
                UIM.RefreshTokens();
                isMovingorRotating = false;
                unitMaterialModel.GetComponent<SkinnedMeshRenderer>().material = initMaterial;
                hasUsedExtraTurn = false;
            }
            else
            {
                turnsWithBuffOrDebuff--;
                if (turnsWithBuffOrDebuff <= 0)
                {
                    BuffbonusStateDamage = 0;
                }
                if (arrowIndicator != null)
                {
                    arrowIndicator.SetActive(false);
                }
                hasMoved = true;

                if (movementTokenInGame != null)
                {
                    movementTokenInGame.SetActive(false);
                }
                hasAttacked = true;
                if (attackTokenInGame != null)
                {
                    attackTokenInGame.SetActive(false);
                }
                //Refresco de los tokens para resetearlos en pantalla
                UIM.RefreshTokens();
                isMovingorRotating = false;
                unitMaterialModel.GetComponent<SkinnedMeshRenderer>().material = finishedMaterial;
                hasUsedExtraTurn = false;

                if (turnStunned <= 0)
                {
                    isStunned = false;
                    turnStunned = 0;
                }
                turnStunned--;
            }

        }

    }

    //La unidad ha atacado y por tanto no puede hacer nada más.
    private void FinishMyActions()
    {
        if (arrowIndicator != null)
        {
            arrowIndicator.SetActive(false);
        }
        
        //La unidad ha atacado
        hasAttacked = true;
		hasMoved = true;
		//Refresco de los tokens de ataque
		UIM.RefreshTokens();
		//Aviso al LM que deseleccione la unidad
		LM.DeSelectUnit();
        UIM.ActivateDeActivateEndButton();
        //Doy feedback de que esa unidad no puede hacer nada
        unitMaterialModel.GetComponent<SkinnedMeshRenderer>().material = finishedMaterial;
    }

    #endregion

    #region INTERACTION

    //Al clickar en una unidad aviso al LM
    //Es virtual para el decoy del mago.
    protected virtual void OnMouseDown()
    {
        if (LM.selectedCharacter == this)
        {
            LM.TileClicked(this.myCurrentTile);
        }
        else
        {
            Valkyrie valkyrieRef = FindObjectOfType<Valkyrie>();
            if (valkyrieRef != null && LM.selectedCharacter == valkyrieRef && !valkyrieRef.hasMoved && valkyrieRef.changePositions)
            {
                if (currentHealth <= valkyrieRef.numberCanChange)
                {
                    valkyrieRef.ChangePosition(this);
                }
 
            }
            else
            {
                LM.SelectUnit(movementUds, this);
            }
           
        }
    }

    //Es virtual para el decoy del mago.
    protected virtual void OnMouseEnter()
    {
        if (LM.currentLevelState == LevelManager.LevelState.ProcessingPlayerActions)
        {
            if (LM.selectedEnemy == null)
            {
                if (LM.selectedCharacter != null && LM.selectedCharacter.currentUnitsAvailableToAttack.Contains(this.GetComponent<UnitBase>()))
                {
                    Cursor.SetCursor(LM.UIM.attackCursor, Vector2.zero, CursorMode.Auto);
                }
                if (LM.selectedCharacter != null && !LM.selectedCharacter.currentUnitsAvailableToAttack.Contains(this.GetComponent<UnitBase>()))
                {
                    myPanelPortrait.GetComponent<Portraits>().HighlightPortrait();
                }

                if (!hasAttacked)
                {
                    myPanelPortrait.GetComponent<Portraits>().HighlightPortrait();
                    SelectedColor();
                    LM.ShowUnitHover(movementUds, this);
                }
            }

            else if (LM.selectedEnemy != null)
            {
                if (LM.selectedCharacter == null)
                {
                    myPanelPortrait.GetComponent<Portraits>().HighlightPortrait();
                    SelectedColor();
                }
            
            }
        }
    }

    //Es virtual para el decoy del mago.
    protected virtual void OnMouseExit()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        if (LM.currentLevelState != LevelManager.LevelState.Initializing)
        {
            if (LM.selectedCharacter == null)
            {
                //Compruebo si no hay un enemigo seleccionado para no quitarle la info.
                if (LM.selectedEnemy == null)
                {
                    LM.HideUnitHover(this);
                }

                myPanelPortrait.GetComponent<Portraits>().UnHighlightPortrait();

                ResetColor();
            }

            else if (LM.selectedCharacter == this)
            {
                return;
            }

            else if (LM.selectedCharacter != GetComponent<PlayerUnit>())
            {
                LM.HideUnitHover(this);
                myPanelPortrait.GetComponent<Portraits>().UnHighlightPortrait();

                ResetColor();
            }
        }

        //Quito el healthbar de los objetivos a los que puedo atacar al salir del hover
        //Aunque lo desactivo en el hover exit, se activan en el CheckUnits en vez de en el hover enter
        for (int i = 0; i < currentUnitsAvailableToAttack.Count; i++)
        {
            currentUnitsAvailableToAttack[i].HealthBarOn_Off(false);
        }
    }

    #endregion

    #region MOVEMENT_&_ROTATION

    //El LevelManager avisa a la unidad de que debe moverse.
    public virtual void MoveToTile(IndividualTiles tileToMove, List<IndividualTiles> pathReceived)
    {
        //Compruebo la dirección en la que se mueve para girar a la unidad
        //   CheckTileDirection(tileToMove);
        hasMoved = true;
        movementTokenInGame.SetActive(false);
        //Refresco los tokens para reflejar el movimiento
        UIM.RefreshTokens();

        //Limpio myCurrentPath y le añado las referencias de pathReceived 
        //(Como en el goblin no vale con hacer myCurrentPath = PathReceived porque es una referencia a la lista y necesitamos una referencia a los elementos dentro de la lista)
        myCurrentPath.Clear();

        for (int i = 0; i < pathReceived.Count; i++)
        {
            myCurrentPath.Add(pathReceived[i]);
        }

        if (myCurrentPath.Count > 0)
        {
            StartCoroutine("MovingUnitAnimation");
            UpdateInformationAfterMovement(tileToMove);
        }
        else
        {
            isMovingorRotating = false;
            LM.UnitHasFinishedMovementAndRotation();
            UpdateInformationAfterMovement(myCurrentTile);
        }
    }

    IEnumerator MovingUnitAnimation()
    {
        //Activo el trail de particulas de movimiento
        movementParticle.SetActive(true);

        //isMovingorRotating = true;

        if(myCurrentPath.Count > 0)
        {

            //Animación de movimiento
            for (int j = 1; j < myCurrentPath.Count; j++)
            {
                SoundManager.Instance.PlaySound(AppSounds.MOVEMENT);

                //Calcula el vector al que se tiene que mover.
                currentTileVectorToMove = myCurrentPath[j].transform.position; // new Vector3(myCurrentPath[j].transform.position.x, myCurrentPath[j].transform.position.y, myCurrentPath[j].transform.position.z);

                //Muevo y roto a la unidad
                unitModel.transform.DOLookAt(currentTileVectorToMove, timeDurationRotation, AxisConstraint.Y);
                transform.DOMove(currentTileVectorToMove, timeMovementAnimation);
                

                //Espera entre casillas
                yield return new WaitForSeconds(timeMovementAnimation);
            }

            //Desactivo el trail de partículas de movimiento
            movementParticle.SetActive(false);

        }

        //Arriba o abajo
        if (currentFacingDirection == FacingDirection.North)
        {
            unitModel.transform.DORotate(new Vector3(0, 0, 0), timeDurationRotation);
        }

        else if (currentFacingDirection == FacingDirection.South)
        {
            unitModel.transform.DORotate(new Vector3(0, 180, 0), timeDurationRotation);
        }

        else if (currentFacingDirection == FacingDirection.East)
        {
            unitModel.transform.DORotate(new Vector3(0, 90, 0), timeDurationRotation);
        }

        else if (currentFacingDirection == FacingDirection.West)
        {
            unitModel.transform.DORotate(new Vector3(0, -90, 0), timeDurationRotation);
        }

        yield return new WaitForSeconds(timeDurationRotation);

        isMovingorRotating = false;
    }

    public void RotateUnitFromButton(FacingDirection newDirection, IndividualTiles _tileToMove, List<IndividualTiles> _currentPath)
    {
        //Arriba o abajo
        if (newDirection == FacingDirection.North)
        {
            unitModel.transform.DORotate(new Vector3(0, 0, 0), timeDurationRotation);
            currentFacingDirection = FacingDirection.North;
        }

        else if (newDirection == FacingDirection.South)
        {
            unitModel.transform.DORotate(new Vector3(0, 180, 0), timeDurationRotation);
            currentFacingDirection = FacingDirection.South;
        }

        else if (newDirection == FacingDirection.East)
        {
            unitModel.transform.DORotate(new Vector3(0, 90, 0), timeDurationRotation);
            currentFacingDirection = FacingDirection.East;
        }

        else if (newDirection == FacingDirection.West)
        {
            unitModel.transform.DORotate(new Vector3(0, -90, 0), timeDurationRotation);
            currentFacingDirection = FacingDirection.West;
        }

        canvasWithRotationArrows.gameObject.SetActive(false);
        
        isMovingorRotating = true;

        MoveToTile(_tileToMove, _currentPath);

        LM.UnitHasFinishedMovementAndRotation();

        //IMPORTANTE: HE PUESTO ESTO DESPUÉS PARA QUE FUNCIONE EL MOSTRAR RANGO DE ATAQUE Y PARECE NO DAR PROBLEMAS. REVISAR EN EL FUTURO
        //Esto tiene que ir antes del  LM.UnitHasFinishedMovementAndRotation() para que función de UnitHasFinishedMovementAndRotation() sepa si hay
        // enemigos a los que atacar
        CheckUnitsAndTilesInRangeToAttack();
    }

    public override void UndoMove(IndividualTiles tileToMoveBack, FacingDirection rotationToTurnBack, bool shouldResetMovement)
    {
        base.UndoMove(tileToMoveBack, rotationToTurnBack, shouldResetMovement);

        if (shouldResetMovement)
        {
            isMovingorRotating = false;
            hasMoved = false;
        }

        UIM.RefreshTokens();
    }

    public override void UndoAttack(int previousHealth)
    {
        //Todo esto es una copia del undo move sin la parte que resetea el movimiento.

        //Permitirle otra vez atacar
        hasAttacked = false;

        //Resetear el material
        ResetColor();

       
        //Base (restaurar vida a nivel lógico)
        base.UndoAttack(previousHealth);
        
        //Actualizar hud
        UIM.RefreshHealth();
        UIM.RefreshTokens();

    }

    #endregion

    #region ATTACK_&_HEALTH

    //Función de ataque que se hace override en cada clase
    public virtual void Attack(UnitBase unitToAttack)
    {
        attackTokenInGame.SetActive(false);

        //El daño y la animación no lo pongo aquí porque tiene que ser lo primero que se calcule.

        //Cada unidad se encargará de aplicar su efecto en su override.


        UIM.CheckActionsAvaliable();
        //La unidad ha atacado y por tanto no puede hacer nada más. Así que espero a que acabe la animación y finalizo su turno.
        StartCoroutine("AttackWait");
    }

    IEnumerator AttackWait()
    {
        yield return new WaitForSeconds(timeWaitAfterAttack);
        FinishMyActions();
    }

    public void CheckIfKnightIsDefending(Knight knightThatDef, UnitBase unitThatIsAttacking)
    {
        if (knightThatDef != null)
        {
            //Este es el valor que queremos que tenga para defender unidades
            knightThatDef.shieldDef = 5;
            if (knightThatDef.isBlockingNeighbours)
            {
                if (knightThatDef.myCurrentTile.neighbours.Contains(myCurrentTile))
                {

                    if ((knightThatDef.currentFacingDirection == FacingDirection.North && unitThatIsAttacking.currentFacingDirection == FacingDirection.South)
                        || (knightThatDef.currentFacingDirection == FacingDirection.South && unitThatIsAttacking.currentFacingDirection == FacingDirection.North)
                        || (knightThatDef.currentFacingDirection == FacingDirection.West && unitThatIsAttacking.currentFacingDirection == FacingDirection.East)
                        || (knightThatDef.currentFacingDirection == FacingDirection.East && unitThatIsAttacking.currentFacingDirection == FacingDirection.West))
                    {

                        //Cambiar variable en el Knight
                        if (knightThatDef.isBlockingNeighboursFull)
                        {
                            knightThatDef.shieldDef = 999;
                        }

                    }
                    else
                    {
                        knightThatDef.shieldDef = 0;
                    }
                }
                else
                {
                    knightThatDef.shieldDef = 0;
                }

            }
            else
            {
                knightThatDef.shieldDef = 0;
            }


        }
    }


    public override void ReceiveDamage(int damageReceived, UnitBase unitAttacker)
    {
        //Animación de ataque
        myAnimator.SetTrigger("Damage");

        //Estas líneas las añado para comprobar si el caballero tiene que defender
        Knight knightDef = FindObjectOfType<Knight>();
        CheckIfKnightIsDefending(knightDef, unitAttacker);
        
        if (knightDef != null)
        {
            damageReceived -= knightDef.shieldDef;
        }
       
        if (damageReceived < 0)
        {
            damageReceived = 0;
        }

        if (currentArmor > 0)
        {
            currentArmor -= damageReceived;
            if (currentArmor < 0)
            {
                damageReceived = currentArmor * -1;
                currentHealth -= damageReceived;
                currentArmor = 0;

            }
        }

        else
        {
            currentHealth -= damageReceived;
        }
       
        Debug.Log("Soy " + name + " me han hecho daño");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        //Cuando me hacen daño refresco la información en la interfaz
        UIM.RefreshHealth();

        base.ReceiveDamage(damageReceived,unitAttacker);
    }

    public override void Die()
    {
        Debug.Log("Soy " + gameObject.name + " y he muerto");

        //Animación de ataque
        myAnimator.SetTrigger("Death");

        Instantiate(deathParticle, gameObject.transform.position, gameObject.transform.rotation);

        myCurrentTile.unitOnTile = null;
        myCurrentTile.WarnInmediateNeighbours();

        LM.charactersOnTheBoard.Remove(this);
        myPanelPortrait.SetActive(false);
        UIM.panelesPJ.Remove(myPanelPortrait);
        Destroy(gameObject);        
    }

    #endregion

    #region FEEDBACK

    //Función que muestra el efecto del ataque y que se hace override en cada clase.
    public virtual void ShowHover(UnitBase enemyToAttack)
    {
        //Cada unidad muestra su efecto
        CalculateDamage(enemyToAttack);
		//Mostrar el daño es común a todos
		//enemyToAttack.EnableCanvasHover(Mathf.RoundToInt(damageWithMultipliersApplied));
    }

    public void SelectedColor()
    {
        unitMaterialModel.GetComponent<SkinnedMeshRenderer>().material = selectedMaterial;
    }

    //Override para que el personaje pueda volver a negro si ya ha atacado
    public override void ResetColor()
    {
        //Si ha atacado vuelve al color negro
        if (hasAttacked)
        {
            unitMaterialModel.GetComponent<SkinnedMeshRenderer>().material = finishedMaterial;
        }

        //Si no vuelve al inicial
        else
        {
            unitMaterialModel.GetComponent<SkinnedMeshRenderer>().material = initMaterial;
        }

    }

	public override void CalculateDamage(UnitBase unitToDealDamage)
	{
		//Reseteo la variable de daño a realizar
		damageWithMultipliersApplied = baseDamage;

		//Si estoy en desventaja de altura hago menos daño
		if (unitToDealDamage.myCurrentTile.height > myCurrentTile.height)
		{
			damageWithMultipliersApplied -= penalizatorDamageLessHeight;
            unitToDealDamage.downToUpDamageIcon.SetActive(true);
		}

		//Si estoy en ventaja de altura hago más daño
		else if (unitToDealDamage.myCurrentTile.height < myCurrentTile.height)
		{
			damageWithMultipliersApplied += bonusDamageMoreHeight;
            unitToDealDamage.upToDownDamageIcon.SetActive(true);
		}

		//Si le ataco por la espalda hago más daño
		if (unitToDealDamage.currentFacingDirection == currentFacingDirection)
		{
            if (unitToDealDamage.GetComponent<EnDuelist>()
               && unitToDealDamage.GetComponent<EnDuelist>().hasTier2
               && hasAttacked)
            {

                if (currentFacingDirection == FacingDirection.North)
                {
                    unitToDealDamage.unitModel.transform.DORotate(new Vector3(0, 180, 0), timeDurationRotation);
                    unitToDealDamage.currentFacingDirection = FacingDirection.South;
                }

                else if (currentFacingDirection == FacingDirection.South)
                {
                    unitToDealDamage.unitModel.transform.DORotate(new Vector3(0, 0, 0), timeDurationRotation);
                    unitToDealDamage.currentFacingDirection = FacingDirection.North;
                }

                else if (currentFacingDirection == FacingDirection.East)
                {

                    unitToDealDamage.unitModel.transform.DORotate(new Vector3(0, -90, 0), timeDurationRotation);
                    unitToDealDamage.currentFacingDirection = FacingDirection.West;
                }

                else if (currentFacingDirection == FacingDirection.West)
                {
                    unitToDealDamage.unitModel.transform.DORotate(new Vector3(0, 90, 0), timeDurationRotation);
                    unitToDealDamage.currentFacingDirection = FacingDirection.East;
                }

            }
            else
            {
                //Añado este if para que, cada vez que ataque un jugador y si le va a realizar daño por la espalda, el count del honor se resetea
                if (hasAttacked)
                {
                    LM.honorCount = 0;
                }
                //Ataque por la espalda
                damageWithMultipliersApplied += bonusDamageBackAttack;
                unitToDealDamage.backStabIcon.SetActive(true);
            }
		}

        //Estas líneas las añado para comprobar si el samurai tiene la mejora de la pasiva 1
        Samurai samuraiUpgraded = FindObjectOfType<Samurai>();

        if (samuraiUpgraded != null && samuraiUpgraded.itsForHonorTime2)
        {
            damageWithMultipliersApplied += LM.honorCount;

        }

        damageWithMultipliersApplied += BuffbonusStateDamage;

        Debug.Log("Daño base: " + baseDamage + " Daño con multiplicadores " + damageWithMultipliersApplied);
	}

	public void HideDamageIcons(UnitBase unitToHide)
	{
        unitToHide.downToUpDamageIcon.SetActive(false);
        unitToHide.upToDownDamageIcon.SetActive(false);
        unitToHide.backStabIcon.SetActive(false);
	}

	#endregion

	#region CHECKS

	//En caso de querer generalizar la comprobación de en que dirección está un tile en comparación a mi posición, lo que se puede hacer es que la función no sea un void, si no que 
	//devuelva un valor de un enum como el de la rotación del personaje, de tal forma que los 4 ifs solo se ponen una vez y siempre devuelven una dirección

	//De momento esta función simplemente sirve para girar al personaje.
	public void CheckTileDirection(IndividualTiles tileToCheck)
    {
        //Arriba o abajo
        if (tileToCheck.tileX == myCurrentTile.tileX)
        {
            //Arriba
            if (tileToCheck.tileZ > myCurrentTile.tileZ)
            {
                unitModel.transform.DORotate(new Vector3(0, 0, 0), timeDurationRotation);
                currentFacingDirection = FacingDirection.North;
            }
            //Abajo
            else
            {
                unitModel.transform.DORotate(new Vector3(0, 180, 0), timeDurationRotation);
                currentFacingDirection = FacingDirection.South;
            }
        }
        //Izquierda o derecha
        else
        {
            //Derecha
            if (tileToCheck.tileX > myCurrentTile.tileX)
            {
                unitModel.transform.DORotate(new Vector3(0, 90, 0), timeDurationRotation);
                currentFacingDirection = FacingDirection.East;
            }
            //Izquierda
            else
            {
                unitModel.transform.DORotate(new Vector3(0, -90, 0), timeDurationRotation);
                currentFacingDirection = FacingDirection.West;
            }
        }
    }

    //Comprueba las unidades (tanto aliadas como enemigas) que están en alcance para ser atacadas.
    //Es virtual porque la comprobación del pícaro es diferente (tiene que tener en cuenta el tile en el que va a acabar tras el salto).
    public virtual void CheckUnitsAndTilesInRangeToAttack()
    {
        currentUnitsAvailableToAttack.Clear();
        currentTilesInRangeForAttack.Clear();

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
                if (!myCurrentTile.tilesInLineUp[i].isEmpty && !myCurrentTile.tilesInLineUp[i].isObstacle && Mathf.Abs(myCurrentTile.tilesInLineUp[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack)
                {
                    currentTilesInRangeForAttack.Add(myCurrentTile.tilesInLineUp[i]);
                }

                if (myCurrentTile.tilesInLineUp[i].unitOnTile != null && Mathf.Abs(myCurrentTile.tilesInLineUp[i].height -myCurrentTile.height) <= maxHeightDifferenceToAttack)
                {
                    //Almaceno la primera unidad en la lista de posibles unidades
                    currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineUp[i].unitOnTile);
                    //tilesInRangeForAttack.Add();
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
                if (!myCurrentTile.tilesInLineDown[i].isEmpty && !myCurrentTile.tilesInLineDown[i].isObstacle && Mathf.Abs(myCurrentTile.tilesInLineDown[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack)
                {
                    currentTilesInRangeForAttack.Add(myCurrentTile.tilesInLineDown[i]);
                }

                if (myCurrentTile.tilesInLineDown[i].unitOnTile != null && Mathf.Abs(myCurrentTile.tilesInLineDown[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack)
                {
                    //Almaceno la primera unidad en la lista de posibles unidades
                    currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineDown[i].unitOnTile);
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
                if (!myCurrentTile.tilesInLineRight[i].isEmpty && !myCurrentTile.tilesInLineRight[i].isObstacle && Mathf.Abs(myCurrentTile.tilesInLineRight[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack)
                {
                    currentTilesInRangeForAttack.Add(myCurrentTile.tilesInLineRight[i]);
                }

                if (myCurrentTile.tilesInLineRight[i].unitOnTile != null && Mathf.Abs(myCurrentTile.tilesInLineRight[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack)
                {
                    //Almaceno la primera unidad en la lista de posibles unidades
                    currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineRight[i].unitOnTile);
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
                if (!myCurrentTile.tilesInLineLeft[i].isEmpty && !myCurrentTile.tilesInLineLeft[i].isObstacle && Mathf.Abs(myCurrentTile.tilesInLineLeft[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack)
                {
                    currentTilesInRangeForAttack.Add(myCurrentTile.tilesInLineLeft[i]);
                }

                if (myCurrentTile.tilesInLineLeft[i].unitOnTile != null && Mathf.Abs(myCurrentTile.tilesInLineLeft[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack)
                {
                    //Almaceno la primera unidad en la lista de posibles unidades
                    currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineLeft[i].unitOnTile);
                    break;
                }
            }	
		}

        //Marco las unidades disponibles para atacar de color rojo
        for (int i = 0; i < currentUnitsAvailableToAttack.Count; i++)
        {
            CalculateDamage(currentUnitsAvailableToAttack[i]);
            currentUnitsAvailableToAttack[i].ColorAvailableToBeAttacked(damageWithMultipliersApplied);
            currentUnitsAvailableToAttack[i].HealthBarOn_Off(true);
            currentUnitsAvailableToAttack[i].myCurrentTile.ColorInteriorRed();
        }


        for (int i = 0; i < currentTilesInRangeForAttack.Count; i++)
        {
            currentTilesInRangeForAttack[i].ColorBorderRed();
        }


    }

    #endregion


    //Si estamos haciendo pruebas y no cargamos desde el nivel de mapa tiene que hacer el raycast. 
    //Si probamos desde el mapa de seleccion el tile se setea al colocar las unidades.
    protected override void FindAndSetFirstTile()
    {
        if (LM.FuncionarSinHaberSeleccionadoPersonajesEnEscenaMapa)
        {
            base.FindAndSetFirstTile();
        }
    }

    
}
