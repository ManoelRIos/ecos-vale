using UnityEngine;
using UnityEngine.InputSystem;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        const float BossIntroductionDuration = 3f;
        static readonly string[] BossSubtitles = {
            "A COLHEITA NUNCA TERMINA", "O LUTO APRENDEU A CACAR", "A FE ARDEU ATE VIRAR FOME",
            "TODA PRAGA TEM UMA MAE", "O ULTIMO DOBRE VEM DE BAIXO", "A SENTENCA NAO ENFERRUJA",
            "O BREJO SE LEMBRA DE TUDO"
        };
        static readonly string[] BossPortraitIds = {
            "boss", "widow", "bishop", "matriarch", "bell", "iron-executioner", "bog-oracle"
        };
        static readonly string[] BossChallenges = {
            "Mais uma alma madura. Venha: minha colheita espera!",
            "Fale baixo. Ate os mortos acordam quando eu choro.",
            "Ajoelhe-se. Suas cinzas ainda podem servir ao altar.",
            "Meus filhos sentem seu calor. Nao os deixe com fome.",
            "Escute. O proximo toque ja conhece o seu nome.",
            "A cripta deu seu veredito. Eu apenas seguro o machado.",
            "Vi sua chegada na lama. Ainda nao vi sua partida."
        };
        static readonly string[] BossLoreReplies = {
            "Enterraram a primeira semente sobre um rei. A fome ficou.",
            "Meu veu guarda os nomes de quem prometeu voltar.",
            "Apaguei o sol para salvar meu templo. A chama ficou em mim.",
            "A cidade nos chamou de praga. Eu os chamo de familia.",
            "Nao fui feito para soar. Fui feito para manter algo dormindo.",
            "A ultima ordem foi vigiar. Ninguem veio me dispensar.",
            "O brejo bebe lembrancas. Eu sou tudo o que ele nao esqueceu."
        };

        bool merchantConversationActive, bossIntroductionActive;
        int merchantConversationPage, conversationSelection, conversationOpenedFrame = -1, bossIntroductionVariant;
        int dialogueVisibleCharacters, dialogueVoice;
        float dialogueCharacterClock, conversationArtTime, bossIntroductionElapsed;
        bool dialogueConfirmReady;
        string dialogueLine = "", dialogueSpeaker = "", dialoguePortrait = "merchant";
        string dialogueChoiceOne = "", dialogueChoiceTwo = "";
        Enemy introductionBoss;
        Vector3 introductionCameraStart;

        bool ConversationActive => merchantConversationActive;
        bool BossIntroActive => bossIntroductionActive;
        float BossIntroProgress => Mathf.Clamp01(bossIntroductionElapsed / BossIntroductionDuration);
        bool ConversationCapturesInput => ConversationActive || BossIntroActive;
        bool DialogueFullyRevealed => dialogueVisibleCharacters >= dialogueLine.Length;

        void BeginMerchantConversation()
        {
            if (!Playing || !InShop || Paused || State == null || State.IsDead || BossIntroActive) return;
            if (Vector2.Distance(playerPosition, MerchantPosition) > 2.8f) return;
            merchantConversationActive = true;
            shopMenuOpen = showMap = false;
            merchantConversationPage = 0;
            PrepareConversationInput();
            StopBeam();
            RollEnd();
            playerMoving = false;
            noticeTime = 0;
            Cursor.visible = !UsingController;
            SetDialogueLine("O COLETOR DE ALMAS", "merchant",
                "A cripta cobra em sangue. Eu aceito almas.\nO que procura, viajante?", 0,
                "NEGOCIAR", "PEDIR UMA DICA");
            PlaySound(SoundCue.Door, .3f);
        }

        void BeginBossIntro(Enemy boss)
        {
            if (boss == null || !boss.Sprite || !Playing || InShop || State == null || State.IsDead) return;
            merchantConversationActive = shopMenuOpen = showMap = false;
            bossIntroductionActive = true;
            introductionBoss = boss;
            // Emergence normally advances with combat time. The introduction owns
            // real time, so reveal the guardian before freezing the simulation.
            boss.SpawnRemaining = 0;
            Color visibleBoss = boss.Sprite.color;
            visibleBoss.a = 1;
            boss.Sprite.color = visibleBoss;
            if (boss.Rift) boss.Rift.enabled = false;
            bossIntroductionVariant = Mathf.Clamp(BossVariant, 0, BossPortraitIds.Length - 1);
            bossIntroductionElapsed = 0;
            introductionCameraStart = view ? view.transform.position : new Vector3(0, 0, -20);
            PrepareConversationInput();
            StopBeam();
            RollEnd();
            playerMoving = false;
            noticeTime = 0;
            SetDialogueLine(BossNames[bossIntroductionVariant], BossPortraitIds[bossIntroductionVariant],
                BossChallenges[bossIntroductionVariant], bossIntroductionVariant + 1,
                "DESAFIAR", "QUEM E VOCE?");
            PlayBossIntroImpact();
        }

        void PrepareConversationInput()
        {
            conversationOpenedFrame = Time.frameCount;
            conversationSelection = 0;
            dialogueConfirmReady = Gamepad.current == null || !Gamepad.current.buttonSouth.isPressed;
            ConsumeControl();
        }

        void SetDialogueLine(string speaker, string portrait, string line, int voice, string first, string second)
        {
            dialogueSpeaker = speaker;
            dialoguePortrait = portrait;
            dialogueLine = line;
            dialogueVoice = voice;
            dialogueChoiceOne = first;
            dialogueChoiceTwo = second;
            dialogueVisibleCharacters = 0;
            dialogueCharacterClock = 0;
            conversationSelection = 0;
        }

        void UpdateConversations(float unscaledDt)
        {
            if (!ConversationCapturesInput || Paused) return;
            float dt = float.IsNaN(unscaledDt) || float.IsInfinity(unscaledDt) ? 0 : Mathf.Max(0, unscaledDt);
            conversationArtTime += dt;
            if (BossIntroActive)
            {
                bossIntroductionElapsed = Mathf.Min(BossIntroductionDuration, bossIntroductionElapsed + dt);
                UpdateIntroductionCamera();
            }
            AdvanceDialogueLetters(dt, BossIntroActive ? 54 : 37);
            if (BossIntroActive && bossIntroductionElapsed >= BossIntroductionDuration)
            {
                EndBossIntroduction();
                return;
            }
            if (Time.frameCount == conversationOpenedFrame) return;
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            if (pad != null && !pad.buttonSouth.isPressed) dialogueConfirmReady = true;
            if (ControlsConsumed) return;
            if (ConversationActive && ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                || (pad != null && pad.buttonEast.wasPressedThisFrame)))
            {
                EndMerchantConversation();
                return;
            }
            if (UsingController && pad != null)
            {
                Vector2 step = MenuStep(pad);
                if (step.x != 0 || step.y != 0)
                    conversationSelection = step.x > 0 || step.y < 0 ? 1 : 0;
            }
            if (keyboard != null && (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame))
                conversationSelection = 0;
            if (keyboard != null && (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame))
                conversationSelection = 1;
            int directChoice = keyboard != null && keyboard.digit1Key.wasPressedThisFrame ? 0
                : keyboard != null && keyboard.digit2Key.wasPressedThisFrame ? 1 : -1;
            bool confirm = (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                || (UsingController && pad != null && dialogueConfirmReady && pad.buttonSouth.wasPressedThisFrame);
            if (directChoice >= 0) SelectDialogueChoice(directChoice);
            else if (confirm)
            {
                ConsumeControl();
                if (!DialogueFullyRevealed) RevealDialogueImmediately();
                else SelectDialogueChoice(conversationSelection);
            }
        }

        void AdvanceDialogueLetters(float dt, float lettersPerSecond)
        {
            if (DialogueFullyRevealed) return;
            dialogueCharacterClock += dt * lettersPerSecond;
            int target = Mathf.Min(dialogueLine.Length, Mathf.FloorToInt(dialogueCharacterClock));
            for (int i = dialogueVisibleCharacters; i < target; i++)
                if (char.IsLetterOrDigit(dialogueLine[i])) PlayDialogueBlip(dialogueLine[i], dialogueVoice);
            dialogueVisibleCharacters = target;
        }

        void RevealDialogueImmediately()
        {
            dialogueVisibleCharacters = dialogueLine.Length;
            dialogueCharacterClock = dialogueLine.Length;
        }

        void SelectDialogueChoice(int choice)
        {
            if (!ConversationCapturesInput || Paused || Time.frameCount == conversationOpenedFrame) return;
            conversationSelection = Mathf.Clamp(choice, 0, 1);
            ConsumeControl();
            dialogueConfirmReady = false;
            if (BossIntroActive)
            {
                // Responses change the challenge, never the mandatory three-second intro.
                string reply = choice == 0 ? "ENTAO VENHA. A CRIPTA ESTA OUVINDO."
                    : BossLoreReplies[bossIntroductionVariant];
                SetDialogueLine(BossNames[bossIntroductionVariant], BossPortraitIds[bossIntroductionVariant],
                    reply, bossIntroductionVariant + 1, "DESAFIAR", "QUEM E VOCE?");
                return;
            }
            if (!DialogueFullyRevealed)
            {
                RevealDialogueImmediately();
                return;
            }
            if (choice == 0)
            {
                merchantConversationActive = false;
                shopMenuOpen = true;
                shopWeaponPage = shopChoice = 0;
                noticeTime = 0;
                Cursor.visible = !UsingController;
                return;
            }
            merchantConversationPage++;
            if (merchantConversationPage == 1)
                SetDialogueLine("O COLETOR DE ALMAS", "merchant",
                    "A esquiva protege no impulso. Depois, recupere o folego.\nAs paredes tambem podem ser seu escudo.",
                    0, "NEGOCIAR", "OUVIR SUA HISTORIA");
            else if (merchantConversationPage == 2)
                SetDialogueLine("O COLETOR DE ALMAS", "merchant",
                    "Ja fui um guardiao. Troquei a coroa por este balcao.\nCada alma compra um pouco do meu esquecimento.",
                    0, "NEGOCIAR", "ATE LOGO");
            else EndMerchantConversation();
        }

        void EndMerchantConversation()
        {
            merchantConversationActive = false;
            noticeTime = 0;
            ConsumeControl();
        }

        void UpdateIntroductionCamera()
        {
            if (!view || introductionBoss == null) return;
            if (introductionBoss.Sprite)
                introductionBoss.Sprite.sprite = art.BossFrame(BossPortraitIds[bossIntroductionVariant],
                    bossIntroductionElapsed < 1.8f ? BossAnimationState.Telegraph : BossAnimationState.Attack,
                    Mathf.FloorToInt(bossIntroductionElapsed * 5));
            view.orthographicSize = CameraSize;
            float horizontal = Mathf.Max(0, HalfWidth + 1.5f - CameraSize * Mathf.Max(.5f, view.aspect));
            float vertical = Mathf.Max(0, HalfHeight + 1.5f - CameraSize);
            Vector2 target = introductionBoss.Position + Vector2.up * .65f;
            target.x = Mathf.Clamp(target.x, -horizontal, horizontal);
            target.y = Mathf.Clamp(target.y, -vertical, vertical);
            float pan = Mathf.SmoothStep(0, 1, Mathf.Clamp01(bossIntroductionElapsed / .85f));
            Vector2 position = Vector2.Lerp((Vector2)introductionCameraStart, target, pan);
            view.transform.position = new Vector3(Mathf.Round(position.x * 32) / 32f,
                Mathf.Round(position.y * 32) / 32f, -20);
        }

        void EndBossIntroduction()
        {
            bossIntroductionActive = false;
            bossIntroductionElapsed = BossIntroductionDuration;
            introductionBoss = null;
            if (view) cameraPosition = view.transform.position;
            ConsumeControl();
        }

        void ClearConversations()
        {
            merchantConversationActive = bossIntroductionActive = false;
            introductionBoss = null;
            bossIntroductionElapsed = conversationArtTime = dialogueCharacterClock = 0;
            dialogueVisibleCharacters = merchantConversationPage = conversationSelection = 0;
            dialogueLine = dialogueSpeaker = dialogueChoiceOne = dialogueChoiceTwo = "";
            dialoguePortrait = "merchant";
            conversationOpenedFrame = -1;
            dialogueConfirmReady = false;
        }

        void DrawConversations()
        {
            if (!ConversationCapturesInput || !Playing || State == null || State.IsDead || Paused) return;
            Color accent = BossIntroActive ? BossAccent : Mint;
            Fill(new Rect(0, 0, 1280, 720), new Color(.015f, .01f, .035f, BossIntroActive ? .13f : .38f));
            if (BossIntroActive)
            {
                Fill(new Rect(0, 0, 1280, 72), Color.black);
                Fill(new Rect(0, 648, 1280, 72), Color.black);
                Fill(new Rect(0, 70, 1280, 2), accent);
                Fill(new Rect(0, 648, 1280, 2), accent);
                Text(new Rect(150, 112, 980, 66), BossNames[bossIntroductionVariant], titleStyle,
                    Color.Lerp(accent, Color.white, .4f), TextAnchor.MiddleCenter);
                Text(new Rect(186, 184, 908, 34), BossSubtitles[bossIntroductionVariant], headingStyle,
                    accent, TextAnchor.MiddleCenter);
                DrawDialoguePortrait(new Rect(80, 398, 158, 168), accent);
                DrawDialogueBody(new Rect(260, 398, 940, 168), accent);
                DrawDialogueChoices(260, 582, 940, accent);
                Text(new Rect(376, 674, 528, 24), "A CRIPTA PRENDE A RESPIRACAO", smallStyle, Muted, TextAnchor.MiddleCenter);
            }
            else
            {
                Panel(new Rect(64, 398, 1152, 270), accent);
                DrawDialoguePortrait(new Rect(82, 416, 148, 168), accent);
                DrawDialogueBody(new Rect(250, 416, 942, 168), accent);
                DrawDialogueChoices(250, 602, 942, accent);
                Text(new Rect(86, 603, 142, 42), UsingController ? "X AVANCAR\nO SAIR" : "E AVANCAR\nESC SAIR",
                    smallStyle, Muted);
            }
        }

        void DrawDialoguePortrait(Rect rect, Color accent)
        {
            Panel(rect, accent);
            Fill(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.height - 16),
                Color.Lerp(new Color(.055f, .045f, .1f), accent, .09f));
            int frame = Mathf.FloorToInt(conversationArtTime * 3);
            Sprite portrait = BossIntroActive
                ? art.BossFrame(dialoguePortrait, BossAnimationState.Telegraph, frame)
                : art.Frame(dialoguePortrait, frame);
            if (portrait)
                GUI.DrawTexture(new Rect(rect.x + 14, rect.y + 13, rect.width - 28, rect.height - 26),
                    portrait.texture, ScaleMode.ScaleToFit, true);
            Fill(new Rect(rect.x + 4, rect.y + 4, 8, 2), Color.Lerp(accent, Color.white, .5f));
            Fill(new Rect(rect.x + 4, rect.y + 4, 2, 8), Color.Lerp(accent, Color.white, .5f));
        }

        void DrawDialogueBody(Rect rect, Color accent)
        {
            Panel(rect, Color.Lerp(accent, Ink, .4f));
            Text(new Rect(rect.x + 20, rect.y + 15, rect.width - 40, 30), dialogueSpeaker, headingStyle, accent);
            string visible = dialogueVisibleCharacters >= dialogueLine.Length ? dialogueLine : dialogueLine.Substring(0, dialogueVisibleCharacters);
            Text(new Rect(rect.x + 20, rect.y + 57, rect.width - 40, rect.height - 75), visible, bodyStyle, Color.white);
            if (!DialogueFullyRevealed && Mathf.FloorToInt(conversationArtTime * 4) % 2 == 0)
                Fill(new Rect(rect.xMax - 19, rect.yMax - 15, 5, 5), accent);
        }

        void DrawDialogueChoices(float x, float y, float width, Color accent)
        {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && Time.frameCount != conversationOpenedFrame;
            float half = (width - 16) * .5f;
            string first = "[1] " + dialogueChoiceOne;
            string second = "[2] " + dialogueChoiceTwo;
            bool firstClicked = Button(new Rect(x, y, half, 42), first, accent, conversationSelection == 0);
            bool secondClicked = Button(new Rect(x + half + 16, y, half, 42), second,
                Color.Lerp(accent, Muted, .45f), conversationSelection == 1);
            GUI.enabled = wasEnabled;
            if (firstClicked) SelectDialogueChoice(0);
            else if (secondClicked) SelectDialogueChoice(1);
        }
    }
}
