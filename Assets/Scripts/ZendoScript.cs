using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Random = UnityEngine.Random;

public class ZendoScript : MonoBehaviour {
	enum ModuleState {
		INACTIVE, EDIT, CHALLENGE, PASS, FAIL, DONE, ERROR
	}

	private readonly string[] SHAPE_KEYS = { "Circle", "Triangle", "Square" };
	private readonly string[] COLOR_LABELS = { "R", "Y", "B" };

	public Light[] lights;

    public KMSelectable[] gridButtons;
    public KMSelectable[] paletteButtons;
    public KMSelectable yesButton;
    public KMSelectable noButton;
    public KMSelectable readyButton;

	public GameObject gridDisplay;

	public TextMesh gridText;
	public int gridTextColumns = 12;
	public TextMesh brushText;

	public Material[] shapeMaterials;

	public GameObject[] colorblindIndicators;

	public int maxChallenges = 5;

	int moduleId;
	static int totalModules = 0;

	ModuleState state = ModuleState.INACTIVE;

	ZendoRule rule = new DebugRule();
	ZendoSymbol[] grid = new ZendoSymbol[9];
	ZendoSymbol brush = new ZendoSymbol(0, 0);
	List<ZendoSymbol[]> challenges;
	int challengeIndex = 0;
	int activateRevealCounter = 0;

	public readonly string TwitchHelpMessage =
		"!{0} clear (Clear the grid) | " +
		"!{0} [a|b|c][1|2|3] [empty|e] (Erase a single cell) | " +
		"!{0} a1 [r|y|b][c|t|s] (Set a single cell) | " +
		"!{0} a1 rc e yt bs (Set/erase several cells in reading order) | " +
		"!{0} press [y|n|ready] (Press a button or several buttons) | " +
		"!{0} slowpress y y n n (Press buttons slowly)" ;

    void Start() {
        Init();

        GetComponent<KMBombModule>().OnActivate += ActivateModule;
    }

	void Init() {
		moduleId = ++totalModules;

		// show/hide colorblind indicators

		bool enableColorBlind = GetComponent<KMColorblindMode>().ColorblindModeActive;

		foreach (GameObject e in colorblindIndicators) {
			e.SetActive(enableColorBlind);
		}

		// fix light range
		float scalar = transform.lossyScale.x;
		for (var i = 0; i < lights.Length; i++)
			lights[i].range *= scalar;
		
		for (int i = 0; i < gridButtons.Length; i++) {
			KMSelectable b = gridButtons[i];

			int j = i;

			b.OnInteract += delegate () { OnGridPress(j); return false; };
		}

		for (int i = 0; i < paletteButtons.Length; i++) {
			KMSelectable b = paletteButtons[i];

			int j = i;

			b.OnInteract += delegate () { OnPalettePress(j); return false; };
		}

		yesButton.OnInteract += delegate () { OnYNButtonPress(true); return false; }; 
		noButton.OnInteract += delegate () { OnYNButtonPress(false); return false; };
		readyButton.OnInteract += delegate () { OnReadyPress(); return false; };

		NewRule();
		UpdateDisplay();
    }

    void ActivateModule() {
		state = ModuleState.EDIT;

		UpdateDisplay();

		StartCoroutine(ActivateReveal());
    }

	void UpdateDisplay() {
		bool valid = false;

		if (state != ModuleState.ERROR)
			valid = EvaluateRule();

		// grid
		switch (state) {
		case ModuleState.PASS:
		case ModuleState.FAIL:
			gridDisplay.SetActive(false);
			gridText.gameObject.SetActive(true);
			SetGridText(rule.GetDescription());
			break;
		case ModuleState.ERROR:
			gridDisplay.SetActive(false);
			gridText.gameObject.SetActive(true);
			SetGridText("Error :( Press Ready to solve");
			break;
		case ModuleState.DONE:
			gridDisplay.SetActive(false);
			gridText.gameObject.SetActive(false);
			break;
		default:
			gridDisplay.SetActive(true);
			gridText.gameObject.SetActive(false);
			for (int i = 0; i < grid.Length; i++) {
				ZendoSymbol symbol = grid[i];

				if (i >= activateRevealCounter)
					symbol = ZendoSymbol.Empty;

				SetShapeIndicator(gridButtons[i].gameObject, symbol);
			}
			break;
		}

		// lights
		switch (state) {
		case ModuleState.EDIT:
			if (valid) {
				SetButtonLights(0);
			} else {
				SetButtonLights(1);
			}
			break;
		case ModuleState.CHALLENGE:
			SetButtonLights(2);
			break;
		default:
			SetButtonLights(-1);
			break;
		}

		// brush
		switch (state) {
		case ModuleState.EDIT:
			paletteButtons[6].gameObject.SetActive(true);
			brushText.gameObject.SetActive(false);
			SetShapeIndicator(paletteButtons[6].gameObject, brush);
			break;
		case ModuleState.CHALLENGE:
			paletteButtons[6].gameObject.SetActive(false);
			brushText.gameObject.SetActive(true);
			brushText.text = string.Format("{0}/{1}", challengeIndex, challenges.Count);
			break;
		default:
			paletteButtons[6].gameObject.SetActive(false);
			brushText.gameObject.SetActive(false);
			break;
		}
	}

	bool EvaluateRule() {
		try {
			return rule.Evaluate(grid);
		} catch (Exception e) {
			Log(e.ToString());
			SwitchToError();

			throw e;
		}
	}

	public List<ZendoSymbol[]> GenerateExamples(int numPositive, int numNegative) {
		List<ZendoSymbol[]> pos = new List<ZendoSymbol[]>();
		List<ZendoSymbol[]> neg = new List<ZendoSymbol[]>();

		int timeout = 1000;

		while (timeout > 0 && (pos.Count < numPositive || neg.Count < numNegative)) {
			ZendoSymbol[] g = new ZendoSymbol[9];

			int fillCount = Random.Range(3, 10);

			for (int i = 0; i < g.Length; i++) {
				if (Random.value < ((double)fillCount) / ((double)(9 - i))) {
					g[i] = new ZendoSymbol(Random.Range(0, 3), Random.Range(0, 3));
					fillCount--;
				} else {
					g[i] = ZendoSymbol.Empty;
				}
			}

			bool valid = rule.Evaluate(g);

			if (valid) {
				if (pos.Count < numPositive)
					pos.Add(g);
			} else {
				if (neg.Count < numNegative)
					neg.Add(g);
			}

			timeout--;
		}

		if (pos.Count == numPositive && neg.Count == numNegative) {
			//Debug.LogFormat("Generated examples after {0} tries", 1000 - timeout);
		} else {
			Log("Warning: Failed to generate examples");
		}

		pos.AddRange(neg);
		return pos;
	}

	public void DisplayExample(bool valid) {
		List<ZendoSymbol[]> examples;

		if (valid) {
			examples = GenerateExamples(1, 0);
		} else {
			examples = GenerateExamples(0, 1);
		}

		if (examples.Count == 1)
			grid = examples[0];
	}

	public void InitChallenge() {
		challenges = GenerateExamples(maxChallenges - 1, maxChallenges - 1);
		challenges.Shuffle();
		challenges.RemoveRange(maxChallenges, challenges.Count - maxChallenges);

		state = ModuleState.CHALLENGE;
		challengeIndex = 0;

		// This passes the list by reference but it's single use anyway so it's fine
		grid = challenges[challengeIndex];

		UpdateDisplay();
	}

	public IEnumerator ActivateReveal() {
		while (activateRevealCounter < grid.Length) {
			yield return new WaitForSeconds(0.05f);
			activateRevealCounter++;
			UpdateDisplay();
		}
	}

	public void NewRule() {
		try {
			rule = ZendoRuleTable.RandomRule();
			LogFormat("The rule is: {0}", rule.GetDescription());
			DisplayExample(true);
		} catch (Exception e) {
			Log(e.ToString());
			SwitchToError();

			throw e;
		}
	}

	public IEnumerator SwitchToPass() {
		Log("Solved!");

		GetComponent<KMBombModule>().HandlePass();
		//GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
		GetComponent<KMAudio>().PlaySoundAtTransform("Solve", transform);

		state = ModuleState.PASS;
		UpdateDisplay();

		yield return new WaitForSeconds(5);

		state = ModuleState.DONE;
		UpdateDisplay();

		yield break;
	}

	public IEnumerator SwitchToFail() {
		GetComponent<KMBombModule>().HandleStrike();

		state = ModuleState.FAIL;
		UpdateDisplay();

		yield return new WaitForSeconds(3);

		NewRule();
		state = ModuleState.EDIT;
		UpdateDisplay();
	}

	public void SwitchToError() {
		state = ModuleState.ERROR;
		UpdateDisplay();
	}

	void OnGridPress(int n) {
		if (state == ModuleState.EDIT) {
			grid[n] = brush;

			if (grid[n].empty) {
				GetComponent<KMAudio>().PlaySoundAtTransform("BeepOff", transform);
			} else {
				GetComponent<KMAudio>().PlaySoundAtTransform("BeepOn", transform);
			}
		}

		UpdateDisplay();
	}

	void OnPalettePress(int n) {
		if (state != ModuleState.EDIT)
			return;

		if (n == 6) {
			// Erase button

			if (brush.empty) {
				for (int i = 0; i < grid.Length; i++) {
					grid[i] = ZendoSymbol.Empty;
				}
				GetComponent<KMAudio>().PlaySoundAtTransform("Erase", transform);
			} else {
				brush.empty = true;
				GetComponent<KMAudio>().PlaySoundAtTransform("BeepOff", transform);
			}
		} else if (n < 3) {
			OnButtonPress();
			brush.shape = n;
		} else {
			OnButtonPress();
			brush.color = n - 3;
		}

		UpdateDisplay();
	}

	void OnYNButtonPress(bool yes) {
		OnButtonPress();

		switch (state) {
		case ModuleState.EDIT:
			DisplayExample(yes);
			break;
		case ModuleState.CHALLENGE:
			bool actual = EvaluateRule();
			if (yes == actual) {
				challengeIndex++;

				if (challengeIndex == challenges.Count) {
					StartCoroutine(SwitchToPass());
				} else {
					// This passes the list by reference but it's single use anyway so it's fine
					grid = challenges[challengeIndex];
				}
			} else {
				string[] gridLog = ZendoSymbol.GridToString(grid).Split('\n');

				Log("The grid was:");
				foreach (string line in gridLog) {
					Log(line);
				}
				LogFormat("This was {0} but you pressed {1} - Strike!", actual ? "valid" : "invalid", yes ? "Y" : "N");

				StartCoroutine(SwitchToFail());
			}
			break;
		}

		UpdateDisplay();
	}

	void OnReadyPress() {
		OnButtonPress();

		switch (state) {
		case ModuleState.EDIT:
			state = ModuleState.CHALLENGE;
			InitChallenge();
			//NewRule();
			break;
		case ModuleState.CHALLENGE:
			state = ModuleState.EDIT;
			break;
		case ModuleState.ERROR:
			GetComponent<KMBombModule>().HandlePass();
			break;
		}

		UpdateDisplay();	
	}

	void SetButtonLights(int index) {
		bool yesOn = (index == 0);
		bool noOn = (index == 1);
		bool readyOn = (index == 2);

		yesButton.transform.Find("GlowOn").gameObject.SetActive(yesOn);
		yesButton.transform.Find("GlowOff").gameObject.SetActive(!yesOn);

		noButton.transform.Find("GlowOn").gameObject.SetActive(noOn);
		noButton.transform.Find("GlowOff").gameObject.SetActive(!noOn);

		readyButton.transform.Find("GlowOn").gameObject.SetActive(readyOn);
		readyButton.transform.Find("GlowOff").gameObject.SetActive(!readyOn);
	}

	void SetShapeIndicator(GameObject obj, ZendoSymbol symbol) {
		obj = obj.transform.Find("Shapes").gameObject;

		if (symbol.empty) {
			obj.SetActive(false);
			return;
		}

		obj.SetActive(true);

		obj.transform.Find("ColorText").GetComponent<TextMesh>().text = COLOR_LABELS[symbol.color];

		int shape = symbol.shape;
		int color = symbol.color;

		for (int i = 0; i < 3; i++) {
			string currentKey = SHAPE_KEYS[i];
			Transform currentShape = obj.transform.Find(currentKey);

			if (!currentShape) {
				LogFormat("Error: Couldn't find shape {0} in indicator", currentKey);
				continue;
			}

			if (shape == i) {
				currentShape.gameObject.SetActive(true);
				currentShape.GetComponent<MeshRenderer>().material = shapeMaterials[color];
			} else {
				currentShape.gameObject.SetActive(false);
			}
		}
	}

	void SetGridText(string text) {
		string[] tokens = text.Split(' ');
		StringBuilder sb = new StringBuilder();

		int lineLength = 0;

		foreach (string token in tokens) {
			if (lineLength + 1 + token.Length > gridTextColumns) {
				sb.Append("\n");
				lineLength = 0;
			}

			if (lineLength > 0) {
				sb.Append(" ");
				lineLength++;
			}

			sb.Append(token);
			lineLength += token.Length;
		}

		gridText.text = sb.ToString();
	}

	void OnButtonPress() {
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		GetComponent<KMSelectable>().AddInteractionPunch(0.5f);
	}

	public IEnumerator ProcessTwitchCommand(string command) {
		List<KMSelectable> buttons = new List<KMSelectable>();
		bool slowPress = false;

		string[] parts = command.ToLowerInvariant().Split(' ');

		if (parts.Length < 1) {
			yield return "sendtochaterror Specify a command";
			yield break;
		}

		if (parts.Length > 21) {
			yield return "sendtochaterror Command too long";
			yield break;
		}

		if (parts[0] == "press" || parts[0] == "slowpress") {
			// Press a button / buttons

			if (parts[0] == "slowpress") {
				slowPress = true;

				if (parts.Length > 11) {
					yield return "sendtochaterror Maximum 10 commands for slowpress, sorry";
					yield break;
				}
			}

			if (parts.Length < 2) {
				yield return "sendtochaterror Specify a button (y|n|ready)";
				yield break;
			}

			for (int i = 1; i < parts.Length; i++) {
				switch (parts[i]) {
				case "y":
				case "yes":
					buttons.Add(yesButton);
					break;
				case "n":
				case "no":
					buttons.Add(noButton);
					break;
				case "r":
				case "ready":
					buttons.Add(readyButton);
					break;
				default:
					yield return "sendtochaterror Valid buttons are y, n, ready";
					yield break;
				}
			}
		} else if (parts[0] == "clear") {
			// Clear the grid
			if (!brush.empty)
				buttons.Add(paletteButtons[6]);
			buttons.Add(paletteButtons[6]);
		} else if (parts[0].Length == 2) {
			// Set grid cells

			if (state != ModuleState.EDIT) {
				yield return "sendtochaterror Can't edit the grid right now";
				yield break;
			}

			int currentCell;

			switch (parts[0][0]) {
			case 'a':
				currentCell = 0;
				break;
			case 'b':
				currentCell = 1;
				break;
			case 'c':
				currentCell = 2;
				break;
			default:
				yield return "sendtochaterror Invalid starting coordinate";
				yield break;
			}

			switch (parts[0][1]) {
			case '1':
				break;
			case '2':
				currentCell += 3;
				break;
			case '3':
				currentCell += 6;
				break;
			default:
				yield return "sendtochaterror Invalid starting coordinate";
				yield break;
			}

			if (parts.Length < 2) {
				yield return "sendtochaterror Specify symbols ([r|y|b][c|s|t] | e)";
				yield break;
			}

			int index = 1;

			ZendoSymbol simulationBrush = brush;

			while (currentCell < 9 && index < parts.Length) {
				string part = parts[index];

				if (part == "empty" || part == "e") {
					if (!simulationBrush.empty) {
						buttons.Add(paletteButtons[6]);
						simulationBrush = ZendoSymbol.Empty;
					}
				} else if (part.Length == 2) {
					int targetColor;
					int targetShape;

					switch (part[0]) {
					case 'r':
						targetColor = 0;
						break;
					case 'y':
						targetColor = 1;
						break;
					case 'b':
						targetColor = 2;
						break;
					default:
						yield return "sendtochaterror Valid colors are r, y, b";
						yield break;
					}

					switch (part[1]) {
					case 'c':
						targetShape = 0;
						break;
					case 't':
						targetShape = 1;
						break;
					case 's':
						targetShape = 2;
						break;
					default:
						yield return "sendtochaterror Valid shapes are c, t, s";
						yield break;
					}

					if (simulationBrush.empty || simulationBrush.shape != targetShape) {
						buttons.Add(paletteButtons[targetShape]);
						simulationBrush.shape = targetShape;
					}

					if (simulationBrush.empty || simulationBrush.color != targetColor) {
						buttons.Add(paletteButtons[targetColor + 3]);
						simulationBrush.color = targetColor;
					}
				} else {
					yield return "sendtochaterror Please specify symbols as [color][shape], \"e\", or \"empty\", separated by spaces";
					yield break;
				}

				buttons.Add(gridButtons[currentCell]);

				index++;
				currentCell++;
			}
		} else {
			yield return "sendtochaterror Specify a coordinate followed by symbols, \"clear\", or \"press\" followed by a button";
			yield break;
		}

		if (slowPress) {
			foreach (KMSelectable button in buttons) {
				yield return "trycancelsequence";
				yield return new KMSelectable[] {button};
				yield return "trywaitcancel 2";
			}
		} else {
			yield return "trycancelsequence";
			yield return buttons;
		}
		yield break;
	}

	public void TwitchHandleForcedSolve() {
		StartCoroutine(SwitchToPass());
	}

	void Log(string s) {
		Debug.LogFormat("[Zendo #{0}] {1}", moduleId, s);
	}

	void LogFormat(string format, params object[] args) {
		string s = string.Format(format, args: args);
		Debug.LogFormat("[Zendo #{0}] {1}", moduleId, s);
	}
}
