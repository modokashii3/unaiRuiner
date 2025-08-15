
using ImGuiNET;
using SharpPluginLoader.Core;
using SharpPluginLoader.Core.Entities;
using System.ComponentModel.Design;
using System.Numerics;

namespace unaiRuiner
{
    public class UnchainedRuiner : IPlugin
    {
        public string Name => "Unchained 悉くを殲ぼす滅尽龍";
        public string Author => "seka";

        private bool _inQuest = false;
        private bool _oneRuinerDies = false;
        private Monster? _ruinerOne = null;
        private uint _lastStage = 0;
        private int _previousAction;
        private int _frameCounter;

        private bool _attackOneCheck = false;
        private bool _supered = false; //once per quest
        private bool _gambit = false; //once per quest
        private float _currentHp;

        private bool _modEnabled = true;
        private string _statusMessage = "";
        private int _frameCountdown = 0;
        private const int _framesForMessage = 60;

        private float _myTimer = 0f;
        private int _timerElapsed = 0;


        private void ResetState()
        {
            // does not include _inQuest
            _ruinerOne = null;
            _oneRuinerDies = false;
            _frameCounter = 0;
            _myTimer = 0f;
            _timerElapsed = 0;
            Monster.EnableSpeedReset();
            _attackOneCheck = false; _supered = false; _gambit = false;
        }
        public void OnMonsterDeath(Monster monster)
        {

            if (_ruinerOne is not null && monster.Type == MonsterType.RuinerNergigante)
            {
                _ruinerOne = null;
                // _oneRuinerDies = false;
                _frameCounter = 0;
                _myTimer = 0f;
                _timerElapsed = 0;
                // Monster.EnableSpeedReset
                _attackOneCheck = false;
            }
        }
        public void OnMonsterDestroy(Monster monster)
        {
            if (_ruinerOne is not null && monster.Type == MonsterType.RuinerNergigante)
            // watch out for using ResetState here, because if ruiner decayed all values are reset
            {
                _ruinerOne = null;
                _oneRuinerDies = false;
                _frameCounter = 0;
                _myTimer = 0f;
                _timerElapsed = 0;
                // Monster.EnableSpeedReset
                _attackOneCheck = false; 
            }
        }
        public void OnQuestEnter(int questId) // different from Quest Accept
        {
            _inQuest = true;
            // do not ResetState because OnMonsterCreate occurs before OnQuestEnter
            // _ruinerOne = null;
            _oneRuinerDies = false;
            _frameCounter = 0;
            _myTimer = 0f;
            _timerElapsed = 0;
            Monster.EnableSpeedReset();
            _attackOneCheck = false; 
        }

        public void OnMonsterCreate(Monster monster)
        {
            _lastStage = (uint)Area.CurrentStage;

            if (monster.Type == MonsterType.RuinerNergigante)
            {
                _ruinerOne = monster;
                _oneRuinerDies = false;
            }
        }

        public void OnQuestLeave(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestComplete(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestFail(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestReturn(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestAbandon(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestAccept(int questId)
        {
            ResetState();
            _inQuest = false;
            // cannot take questId yet
        }
        public void OnQuestCancel(int questId) { ResetState(); _inQuest = false; }

        public void OnMonsterAction(Monster monster, ref int actionId) // the actionId on initiation was already called
        {
            var player = Player.MainPlayer;
            if (player is null) return;
            if (!_modEnabled) return;
            if (!_inQuest) return;

            if (_ruinerOne is null && monster.Type == MonsterType.RuinerNergigante)
            {
                _ruinerOne = monster; // in case for multiple ruiner, the next one alive will carry on the function
            }

            if (monster.Type != MonsterType.RuinerNergigante) return;

            if (_ruinerOne is not null && _ruinerOne.Type == MonsterType.RuinerNergigante)
            {
                _currentHp = _ruinerOne.Health / _ruinerOne.MaxHealth * 100f;
                if (_currentHp <= 40 && !_supered)  {
                    actionId = Actions.SUPER_MODE_BEGIN;
                    _supered = true;
                }

                if (_currentHp <= 6 && !_gambit)  {
                    actionId = Actions.FLYING_ATTACK;
                    _gambit = true;
                }

                if (_currentHp <= 6 && _previousAction == Actions.TO_FLYING_ATTACK && _gambit)
                {
                    actionId = Actions.TO_SPECIAL_HEAD_JUMP;
                }

                if (actionId == Actions.DISCOVER) {
                    _attackOneCheck = true;
                } 
                else if (_attackOneCheck) {
                    switch (_previousAction) {
                        case Actions.DISCOVER:
                            actionId = Actions.TO_FLYING_ATTACK_JUMP;
                            break;
                        case Actions.TO_FLYING_ATTACK_JUMP:
                            actionId = Actions.TO_FLYING_ATTACK;
                            break;
                        case Actions.TO_FLYING_ATTACK:
                            actionId = Actions.TO_SPECIAL_HEAD_JUMP;
                            break;
                        case Actions.TO_SPECIAL_HEAD_JUMP:
                            _attackOneCheck = false;
                            break;
                    }
                }

                if (actionId == Actions.DAMAGE_STUN || actionId == Actions.DAMAGE_STUN_FLY_LOOP) {
                    _attackOneCheck = true;
                }
                else if (_attackOneCheck) {
                    switch (_previousAction) {
                        case Actions.DAMAGE_STUN:
                            actionId = Actions.QUICK_TURN;
                            break;
                        case Actions.DAMAGE_STUN_FLY_LOOP:
                            actionId = Actions.QUICK_TURN;
                            break;
                        case Actions.QUICK_TURN:
                            _attackOneCheck = false;
                            break;
                    }
                }

                if (actionId == Actions.TO_BLACK_THORN_BREAK_ARM_L_ATTACK2 || actionId == Actions.TO_BLACK_THORN_BREAK_ARM_R_ATTACK2) {
                    _attackOneCheck = true;
                }
                else if (_attackOneCheck) {
                    switch (_previousAction) {
                        case Actions.TO_BLACK_THORN_BREAK_ARM_L_ATTACK2:
                            actionId = Actions.JUMP_PUNCH_WING_MODE;
                            break;
                        case Actions.TO_BLACK_THORN_BREAK_ARM_R_ATTACK2:
                            actionId = Actions.JUMP_PUNCH_WING_MODE;
                            break;
                        case Actions.JUMP_PUNCH_WING_MODE:
                            _attackOneCheck = false;
                            break;
                    }
                }

                if (actionId == Actions.TIRED) {
                    actionId = Actions.REFRESH;
                }

                if (_previousAction >= 93 && _previousAction <= 145 && _attackOneCheck)  {
                    _attackOneCheck = false; 
                }

                if (_previousAction >= 201 && _previousAction <= 217 && _attackOneCheck) {
                    _attackOneCheck = false;
                }

                _frameCounter = 0;
                _myTimer = 0f;
                _timerElapsed = 0;
                _previousAction = actionId;
                Monster.EnableSpeedReset();
            }
        }

        public unsafe void OnImGuiRender()
        {
            var player = Player.MainPlayer;
            if (player == null) return;

            if (ImGui.Button("Toggle")) {
                if (Quest.CurrentQuestId == -1) {
                    _modEnabled = !_modEnabled;
                    _statusMessage = _modEnabled ? "Plugin Enabled." : "Plugin Disabled.";
                    ResetState();

                } 
                else  {
                    _statusMessage = "Cannot toggle while in quest.";
                } 
                _frameCountdown = _framesForMessage;
            }
            if (_frameCountdown > 0) {
                ImGui.Text(_statusMessage);
            }
        }

        public void OnUpdate(float deltaTime)
        {
            var player = Player.MainPlayer;
            if (player is null) return;

            if ((uint)Area.CurrentStage != _lastStage)
            {
                ResetState();
            }

            if (!_modEnabled) return;
            if (!_inQuest) return;

            if (_ruinerOne is null) return;
            if (_ruinerOne.Type != MonsterType.RuinerNergigante) return;

            var currentActionId = _ruinerOne?.ActionController.CurrentAction.ActionId;

            if (_ruinerOne is null) return;

            if (currentActionId == Actions.DISCOVER) {
                Monster.DisableSpeedReset();
                _ruinerOne.Speed = 1.4f;
            }

            if (currentActionId == Actions.TO_FLYING_ATTACK) {
                Monster.DisableSpeedReset();
                _ruinerOne.Speed = 1.4f;
                _myTimer += deltaTime;
                if (_myTimer >= 0.1f) {
                    _myTimer -= 0.1f;
                    _timerElapsed++;
                    if (_timerElapsed == 17 && _attackOneCheck) {
                        Log.Info($"ToFlyingAttack");
                        _ruinerOne.AnimationFrame = _ruinerOne.MaxAnimationFrame;
                    }
                    if (_timerElapsed == 22 && _gambit) {
                        Log.Info($"GambitToFlyingAttack");
                        _ruinerOne.AnimationFrame = _ruinerOne.MaxAnimationFrame;
                    }
                }
            }

            if (currentActionId == Actions.TO_SPECIAL_WING_L_ATTACK || currentActionId == Actions.TO_SPECIAL_ARM_L_ATTACK) {
                _myTimer += deltaTime;
                if (_myTimer >= 0.1f) {
                    _myTimer -= 0.1f;
                    _timerElapsed++;
                    if (_timerElapsed == 23) {
                        Log.Info($"ToSpecialWing/Arm L");
                        _ruinerOne.ForceAction(Actions.STRONG_LARM_COMBO); //These COMBO behave differently based on preceding attack
                    }
                }
            }

            if (currentActionId == Actions.TO_SPECIAL_WING_R_ATTACK) {
                _myTimer += deltaTime;
                if (_myTimer >= 0.1f) {
                    _myTimer -= 0.1f;
                    _timerElapsed++;
                    if (_timerElapsed == 23) {
                        Log.Info($"ToSpecialWing R");
                        _ruinerOne.ForceAction(Actions.STRONG_RARM_ATATCK_TO_LEFT);
                    }
                }
            }

            if (currentActionId == Actions.TO_SPECIAL_ARM_R_ATTACK)  {
                _myTimer += deltaTime;
                if (_myTimer >= 0.1f) {
                    _myTimer -= 0.1f;
                    _timerElapsed++;
                    if (_timerElapsed == 23)  {
                        Log.Info($"ToSpecialWing R");
                        _ruinerOne.ForceAction(Actions.TO_RARM_SIDE_SWING);
                    }
                }
            }

            if (currentActionId == Actions.TO_BLACK_THORN_BREAK_ARM_L_ATTACK2 || currentActionId == Actions.TO_BLACK_THORN_BREAK_ARM_R_ATTACK2) {
                _myTimer += deltaTime;
                if (_myTimer >= 0.1f) {
                    _myTimer -= 0.1f;
                    _timerElapsed++;
                    if (_timerElapsed == 9) {
                        Log.Info($"ToBlackThornBreak2");
                        _ruinerOne.AnimationFrame = _ruinerOne.MaxAnimationFrame;
                    }
                }
            }

            if (currentActionId == Actions.DAMAGE_STUN || currentActionId == Actions.DAMAGE_STUN_FLY_LOOP) {
                _myTimer += deltaTime;
                if (_myTimer >= 0.1f) {
                    _myTimer -= 0.1f;
                    _timerElapsed++;
                    if (_timerElapsed == 20) {
                        Log.Info($"Stun");
                        _ruinerOne.AnimationFrame = _ruinerOne.MaxAnimationFrame;
                    }
                }
            }

            if (_previousAction >= 201 && _previousAction <= 217) { //strong part break
                Monster.DisableSpeedReset();
                _ruinerOne.Speed = 1.66f;
            }

            // CurrentAction is a property of type ActionInfo&, and it has a getter({ get; }) which means it can only be read, not set directly.

            // The & symbol here suggests that CurrentAction returns a reference to an ActionInfo object rather than a copy of it.

            // ActionId is a field of ActionInfo, holding the value for CurrentAction

            if (_oneRuinerDies)
            {
                ResetState();
            }
        }
    }
}
