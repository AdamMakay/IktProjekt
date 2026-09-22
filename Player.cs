using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;

	// Gurulással kapcsolatos beállítások
	public const float RollSpeed = 450.0f;
	public const float RollDuration = 0.4f;
	private float _rollTimer = 0.0f;
	private bool _isRolling = false;
	private float _rollDirection = 1.0f;
	private bool _wasQPressed = false;

	// --- TÁMADÁSSAL KAPCSOLATOS BEÁLLÍTÁSOK ---
	public const float AttackDuration = 0.5f;   // Egy egyedi suhintás hossza
	private float _attackTimer = 0.0f;          // Támadási időzítő
	private bool _isAttacking = false;          // Éppen támad-e
	private bool _wasAttackPressed = false;     // Folyamatos nyomvatartás letiltása
	private Area2D _swordArea;                  // Kard Area2D
	private CollisionShape2D _swordCollision;   // Kard ütközője

	// --- KOMBÓ RENDSZER BEÁLLÍTÁSOK (ÚJ) ---
	private int _comboStep = 1;                 // Melyik támadásnál járunk (1 vagy 2)
	private float _comboResetTimer = 0.0f;      // Időzítő, ami figyeli a kombó lejárati idejét
	public const float ComboWindow = 1.0f;      // Hány másodperced van a következő ütésre (pl. 1 másodperc)

	// Hivatkozás az AnimatedSprite2D gyerekcsomópontra
	private AnimatedSprite2D _animatedSprite;

	public override void _Ready()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_swordArea = GetNode<Area2D>("SwordArea");
		_swordCollision = _swordArea.GetNode<CollisionShape2D>("CollisionShape2D");
		_swordCollision.Disabled = true;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// --- KOMBÓ IDŐABLAK CSÖKKENTÉSE (ÚJ) ---
		if (!_isAttacking && _comboStep > 1)
		{
			_comboResetTimer -= (float)delta;
			if (_comboResetTimer <= 0.0f)
			{
				_comboStep = 1; // Ha letelt az idő és nem támadtál, visszaáll az 1. támadásra
			}
		}

		// --- TÁMADÁS IDŐZÍTŐ KEZELÉSE ---
		if (_isAttacking)
		{
			_attackTimer -= (float)delta;
			
			// A támadás közepén aktiváljuk a hitboxot
			if (_attackTimer <= AttackDuration * 0.75f && _attackTimer >= AttackDuration * 0.25f)
			{
				_swordCollision.Disabled = false;
				CheckAttackCollision();
			}
			else
			{
				_swordCollision.Disabled = true;
			}

			if (_attackTimer <= 0.0f)
			{
				_isAttacking = false;
				_swordCollision.Disabled = true;
				
				// ÚJ: Miután véget ért a támadás, elindítjuk a kombó időablakot
				_comboResetTimer = ComboWindow; 
			}
			else
			{
				if (IsOnFloor()) velocity.X = 0;
				Velocity = velocity;
				MoveAndSlide();
				Animate(Vector2.Zero);
				
				_wasQPressed = Input.IsKeyPressed(Key.Q);
				_wasAttackPressed = Input.IsMouseButtonPressed(MouseButton.Left) || Input.IsKeyPressed(Key.F);
				return;
			}
		}

		// --- GURULÁS IDŐZÍTŐ ÉS MOZGÁS ---
		if (_isRolling)
		{
			_rollTimer -= (float)delta;
			if (_rollTimer <= 0.0f)
			{
				_isRolling = false;
			}
			else
			{
				velocity.X = _rollDirection * RollSpeed;
				Velocity = velocity;
				MoveAndSlide();
				Animate(Vector2.Zero);
				_wasQPressed = Input.IsKeyPressed(Key.Q);
				return;
			}
		}

		// Handle Jump.
		if ((Input.IsActionJustPressed("ui_accept") || Input.IsActionJustPressed("move_up")) && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Get the input direction and handle the movement/deceleration.
		Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		// --- GURULÁS INDÍTÁSA KÖZVETLEN Q BILLENTYŰVEL ---
		bool isQCurrentlyPressed = Input.IsKeyPressed(Key.Q);
		if (isQCurrentlyPressed && !_wasQPressed && IsOnFloor() && !_isRolling)
		{
			_isRolling = true;
			_rollTimer = RollDuration;
			_rollDirection = direction.X != 0 ? Mathf.Sign(direction.X) : (_animatedSprite.FlipH ? -1.0f : 1.0f);
			velocity.X = _rollDirection * RollSpeed;
		}
		_wasQPressed = isQCurrentlyPressed;

		// --- TÁMADÁS / KOMBÓ INDÍTÁSA (FRISSÍTVE) ---
		bool isAttackCurrentlyPressed = Input.IsMouseButtonPressed(MouseButton.Left) || Input.IsKeyPressed(Key.F);
		
		if (isAttackCurrentlyPressed && !_wasAttackPressed && IsOnFloor() && !_isRolling && !_isAttacking)
		{
			_isAttacking = true;
			_attackTimer = AttackDuration;
			
			// A kard hitbox helyzetét a lovag nézési irányához igazítjuk
			Vector2 areaPos = _swordArea.Position;
			areaPos.X = _animatedSprite.FlipH ? -Mathf.Abs(areaPos.X) : Mathf.Abs(areaPos.X);
			_swordArea.Position = areaPos;
			
			velocity.X = 0; // Fékezés támadáskor

			// Megnézzük, hogy kombózunk-e vagy az első ütést indítjuk újra
			if (_comboStep == 1)
			{
				_comboStep = 2; // Felkészülünk a következő ütésre, ha időben kattintasz
			}
			else if (_comboStep == 2)
			{
				_comboStep = 1; // A második ütés után visszaáll az elsőre
			}
		}
		_wasAttackPressed = isAttackCurrentlyPressed;

		Velocity = velocity;
		MoveAndSlide();

		// Meghívjuk az animációs logikát a MoveAndSlide után
		Animate(direction);
	}

	private void CheckAttackCollision()
	{
		var OverlappingBodies = _swordArea.GetOverlappingBodies();
		foreach (var body in OverlappingBodies)
		{
			if (body == this) continue;

			if (body.HasMethod("TakeDamage"))
			{
				body.Call("TakeDamage", 1);
			}
		}
	}

	private void Animate(Vector2 direction)
	{
		// --- TÁMADÁS ÉS KOMBÓ ANIMÁCIÓK KEZELÉSE (FRISSÍTVE) ---
		if (_isAttacking)
		{
			// Mivel a _comboStep értékét kattintáskor már megnöveltük, fordítva kell ellenőrizni:
			// Ha a _comboStep már 2, akkor épp az 1-es animáció fut. Ha 1, akkor a 2-es animáció fut.
			if (_comboStep == 2)
			{
				_animatedSprite.Play("attack");
			}
			else
			{
				_animatedSprite.Play("attack2");
			}
			
			_animatedSprite.Offset = Vector2.Zero;
			return;
		}

		// GLOBÁLIS IRÁNYVÁLTÁS
		if (direction.X != 0)
		{
			_animatedSprite.FlipH = direction.X < 0;
		}

		// Ha gurul a karakter
		if (_isRolling)
		{
			_animatedSprite.Play("roll");
			return;
		}
		else
		{
			_animatedSprite.Offset = Vector2.Zero;
		}

		// Ha nincs a földön, akkor a jump animáció fut
		if (!IsOnFloor())
		{
			_animatedSprite.Play("jump");
		}
		// Ha a földön van és mozog vízszintesen
		else if (direction.X != 0)
		{
			_animatedSprite.Play("run");
		}
		// Ha a földön van és áll
		else
		{
			_animatedSprite.Play("idle");
		}
	}
}
