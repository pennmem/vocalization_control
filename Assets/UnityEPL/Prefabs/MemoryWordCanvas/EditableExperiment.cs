using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditableExperiment : MonoBehaviour
{
	public TextDisplayer textDisplayer;
    public TextDisplayer fullscreenTextDisplayer;
    public UnityEngine.UI.InputField inputField;
    public ScriptedEventReporter scriptedEventReporter;
    public SoundRecorder soundRecorder;

    public GameObject microphoneTestMessage;

    public AudioSource audioPlayback;
    public AudioSource highBeep;
    public AudioSource lowBeep;

    private string[] words;

    private const string INSTRUCTIONS_MESSAGE = 
"We will now review the basics of the study, and the experimenter will answer any questions that you have.\n\n1) Words will come onscreen one at a time.\n\n2) After each word, you will see a row of asterisks. While the asterisks are on the screen, say the word you just saw.\n\n3) You may hold down the SPACE BAR to pause the task and take breaks, and RETURN to resume.\n\nIt is very important for you to try to avoid all unnecessary motion while engaged in the study. Please try to limit these activities to the time during the breaks.\n\nYou are now ready to begin the study!\n\nIf you have any remaining questions, please ask the experimenter now.\n\nOtherwise, press RETURN to enter the practice period.";
    private const string BREAK_MESSAGE =
"We will now take some time\nto readjust the electrodes.\nWhen it is time to continue,\npress SPACE and RETURN.";
    private const string EXPERIMENTER_MESSAGE =
"Researcher: Please confirm that the impedance window is closed and that sync pulses are showing.";

	void Start()
	{
        UnityEPL.SetExperimentName("vocalization_control");
        LoadWords();
        StartCoroutine(RunExperiment());
	}

    private void LoadWords()
    {
        words = GetWordpoolLines("wordpool_en");
    }

    private IEnumerator RunExperiment()
    {
        textDisplayer.DisplayText("subject name prompt", "Please enter the subject name and then press enter.");
        yield return new WaitForSeconds(3f);
        textDisplayer.ClearText();
        inputField.gameObject.SetActive(true);
        inputField.Select();
        while (!Input.GetKeyDown(KeyCode.Return))
            yield return null;
        UnityEPL.AddParticipant(inputField.text);
        inputField.gameObject.SetActive(false);
        if (System.IO.Directory.Exists(UnityEPL.GetParticipantFolder()))
        {
            textDisplayer.DisplayText("agnry message", "That participant has already completed this study.");
            yield return new WaitForSeconds(3f);
            textDisplayer.ClearText();
            Quit();
        }
        Cursor.visible = false;

        scriptedEventReporter.ReportScriptedEvent("microphone test begin", new Dictionary<string, object>());
        yield return DoMicrophoneTest();
        scriptedEventReporter.ReportScriptedEvent("microphone test end", new Dictionary<string, object>());

        fullscreenTextDisplayer.textElements[0].alignment = TextAnchor.MiddleLeft;
        yield return PressAnyKey(INSTRUCTIONS_MESSAGE, new KeyCode[] { KeyCode.Return }, fullscreenTextDisplayer);
        fullscreenTextDisplayer.textElements[0].alignment = TextAnchor.MiddleCenter;

        string[] practiceWords = new string[] { "RHINO", "BEAM", "DOG", "WATERMELON", "FLOOD", "MIRROR", "COTTON", "IMAGE", "RING", "VIOLIN" };
        for (int i = 0; i < practiceWords.Length; i++)
        {
            yield return PerformTrial(practiceWords, i, true);
            if (Input.GetKey(KeyCode.Space))
            {
                textDisplayer.DisplayText("resting message", "Resting...");
                while (!Input.GetKeyDown(KeyCode.Return))
                    yield return null;
                textDisplayer.ClearText();
            }
        }

        yield return PressAnyKey("The practice period is complete.  Press RETURN to begin your session.", new KeyCode[] {KeyCode.Return}, textDisplayer);

        for (int i = 0; i < words.Length; i++)
        {
            yield return PerformTrial(words, i, false);
            if (Input.GetKey(KeyCode.Space))
            {
                textDisplayer.DisplayText("resting message", "Resting...");
                while (!Input.GetKeyDown(KeyCode.Return))
                    yield return null;
                textDisplayer.ClearText();
            }
            if (i%192 == 0 && i != 0)
            {
                yield return PressAnyKey(BREAK_MESSAGE, new KeyCode[] { KeyCode.Return, KeyCode.Space }, fullscreenTextDisplayer);
                yield return PressAnyKey(EXPERIMENTER_MESSAGE, new KeyCode[] { KeyCode.Y }, textDisplayer);
            }
        }

        yield return PressAnyKey("The vocalization testing is now complete.\n\nTo finish the session, please take ten minutes to repeat any words that you remember saying today.\n\nPress RETURN to begin.", new KeyCode[] { KeyCode.Return }, fullscreenTextDisplayer);

        //final recall
        string wav_path = System.IO.Path.Combine(UnityEPL.GetDataPath(), "final_recall.wav");
        soundRecorder.StartRecording(wav_path);
        scriptedEventReporter.ReportScriptedEvent("final recall start", new Dictionary<string, object>());
        textDisplayer.DisplayText("final recall prompt", "******");
        yield return new WaitForSeconds(600f);
        textDisplayer.ClearText();
        scriptedEventReporter.ReportScriptedEvent("final recall stop", new Dictionary<string, object>());
        soundRecorder.StopRecording();

        //over
        textDisplayer.DisplayText("end message", "Yay, the session is over!");
    }

    protected IEnumerator DoMicrophoneTest()
    {
        microphoneTestMessage.SetActive(true);
        bool repeat = false;
        string wavFilePath;

        do
        {
            yield return PressAnyKey("Press the spacebar to record a sound after the beep.", new KeyCode[]{KeyCode.Space}, textDisplayer);
            lowBeep.Play();
            textDisplayer.DisplayText("microphone test recording", "Recording...");
            textDisplayer.ChangeColor(Color.red);
            yield return new WaitForSeconds(lowBeep.clip.length);

            wavFilePath = System.IO.Path.Combine(UnityEPL.GetDataPath(), "microphone_test_" + DataReporter.RealWorldTime().ToString("yyyy-MM-dd_HH_mm_ss") + ".wav");
            soundRecorder.StartRecording(wavFilePath);
            yield return new WaitForSeconds(5f);

            soundRecorder.StopRecording();
            textDisplayer.ClearText();

            yield return new WaitForSeconds(1f);

            textDisplayer.DisplayText("microphone test playing", "Playing...");
            textDisplayer.ChangeColor(Color.green);
            audioPlayback.clip = soundRecorder.AudioClipFromDatapath(wavFilePath);
            audioPlayback.Play();
            yield return new WaitForSeconds(5f);
            textDisplayer.ClearText();
            textDisplayer.OriginalColor();

            textDisplayer.DisplayText("microphone test confirmation", "Did you hear the recording? \n(Y=Continue / N=Try Again / C=Cancel).");
            while (!Input.GetKeyDown(KeyCode.Y) && !Input.GetKeyDown(KeyCode.N) && !Input.GetKeyDown(KeyCode.C))
            {
                yield return null;
            }
            textDisplayer.ClearText();

            if (Input.GetKey(KeyCode.C))
                Quit();
            repeat = Input.GetKey(KeyCode.N);
        }
        while (repeat);

        if (!System.IO.File.Exists(wavFilePath))
            yield return PressAnyKey("WARNING: Wav output file not detected.  Sounds may not be successfully recorded to disk.", new KeyCode[] { KeyCode.Return }, textDisplayer);

        microphoneTestMessage.SetActive(false);
    }

    protected IEnumerator PressAnyKey(string displayText, KeyCode[] keyCodes, TextDisplayer pressAnyTextDisplayer)
    {
        yield return null;
        pressAnyTextDisplayer.DisplayText("press any key prompt", displayText);
        while (true)
        {
            yield return null;
            bool done = true;
            foreach (KeyCode keyCode in keyCodes)
            {
                if (!Input.GetKey(keyCode))
                    done = false;
            }
            if (done)
                break;
        }
        pressAnyTextDisplayer.ClearText();
    }

    private IEnumerator PerformTrial(string[] trial_words, int word_index, bool practice)
    {
        //isi
        yield return new WaitForSeconds(Random.Range(0.4f, 0.6f));

        //stimulus
        string stimulus = trial_words[word_index];
        scriptedEventReporter.ReportScriptedEvent("stimulus", new Dictionary<string, object> () { { "word", stimulus }, { "index", word_index } });
        textDisplayer.DisplayText("stimulus", stimulus);
        yield return new WaitForSeconds(1.6f);
        scriptedEventReporter.ReportScriptedEvent("stimulus cleared", new Dictionary<string, object>() { { "word", stimulus }, { "index", word_index } });
        textDisplayer.ClearText();

        //write .lst
        string lst_path = System.IO.Path.Combine(UnityEPL.GetDataPath(), word_index.ToString() + ".lst");
        if (practice)
            lst_path = System.IO.Path.Combine(UnityEPL.GetDataPath(), word_index.ToString() + "_practice.lst");
        WriteAllLinesNoExtraNewline(lst_path, stimulus);

        //isi
        yield return new WaitForSeconds(Random.Range(0.8f, 1.2f));

        //recall
        string wav_path = System.IO.Path.Combine(UnityEPL.GetDataPath(), word_index.ToString() + ".wav");
        if (practice)
            wav_path = System.IO.Path.Combine(UnityEPL.GetDataPath(), word_index.ToString() + "_practice.wav");
        soundRecorder.StartRecording(wav_path);
        scriptedEventReporter.ReportScriptedEvent("recall start", new Dictionary<string, object>() { { "word", stimulus }, { "index", word_index } });
        textDisplayer.DisplayText("recall prompt", "******");
        yield return new WaitForSeconds(2f);
        textDisplayer.ClearText();
        scriptedEventReporter.ReportScriptedEvent("recall stop", new Dictionary<string, object>() { { "word", stimulus }, { "index", word_index } });
        soundRecorder.StopRecording();
    }

    private string[] GetWordpoolLines(string path)
    {
        string text = Resources.Load<TextAsset>(path).text;
        string[] lines = text.Split(new[] { '\r', '\n' });

        Shuffle<string>(new System.Random(), lines);

        return lines;
    }

    //thanks Matt Howells
    private void Shuffle<T> (System.Random rng, T[] array)
    {
        int n = array.Length;
        while (n > 1) 
        {
            int k = rng.Next(n--);
            T temp = array[n];
            array[n] = array[k];
            array[k] = temp;
        }
    }

    //thanks Virtlink from stackoverflow
    protected static void WriteAllLinesNoExtraNewline(string path, params string[] lines)
    {
        if (path == null)
            throw new UnityException("path argument should not be null");
        if (lines == null)
            throw new UnityException("lines argument should not be null");

        using (var stream = System.IO.File.OpenWrite(path))
        {
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(stream))
            {
                if (lines.Length > 0)
                {
                    for (int i = 0; i < lines.Length - 1; i++)
                    {
                        writer.WriteLine(lines[i]);
                    }
                    writer.Write(lines[lines.Length - 1]);
                }
            }
        }
    }
    private void Quit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}