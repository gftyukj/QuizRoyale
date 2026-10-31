using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Royale : MonoBehaviour
{
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void RoyaleState(string json);
    [System.Serializable] class WebState { public string phase,question,feedback; public string[] choices; public int arena,alive,energy,kills,streak; public float health,x,z; }
    public void WebCommand(string command) {
      if(command=="start"){started=true;return;}
      if(!started||transitionAt>0)return;
      if(command=="quiz"&&!quiz&&!map&&!paused)BeginQuiz();
      if(command=="map"&&!quiz&&!paused)map=!map;
      if(command=="pause"&&!quiz&&!map)paused=!paused;
      int n;if(quiz&&!answered&&int.TryParse(command,out n)&&n>=1&&n<=4)Choose(n-1);
    }
    void PublishState(){
      #if UNITY_WEBGL && !UNITY_EDITOR
      RoyaleState(JsonUtility.ToJson(new WebState{phase=!started?"Ready":transitionAt>0?"Next arena":quiz?"Quiz":map?"Map":paused?"Paused":"Exploring",question=quiz?question:"",choices=quiz?choices:new string[0],feedback=quiz&&answered?feedback:"",arena=wave,alive=Alive,energy=ammo,kills=kills,streak=streak,health=me.hp,x=me.P.x,z=me.P.z}));
      #endif
    }
    class Fighter { public GameObject body; public string name; public float hp=100, fire, think; public Vector3 goal; public bool player; public Color color; public Vector3 P { get { return body.transform.position; } set { body.transform.position=value; } } }
    class Cache { public GameObject body; public bool used; }
    readonly List<Fighter> fighters=new List<Fighter>();
    readonly List<Cache> caches=new List<Cache>();
    readonly List<GameObject> world=new List<GameObject>();
    readonly List<Vector3> beacons=new List<Vector3>();
    readonly Dictionary<Color,Material> materials=new Dictionary<Color,Material>();
    Fighter me; Camera cam; LineRenderer ring; Texture2D dot; Font font;
    int wave=1,crowns,streak,kills,ammo=24,correct,total,best; float elapsed,shotAt,quizAt,transitionAt,quizEnd,toastAt;
    bool started,paused,map,quiz,answered; int selected=-1,answer; string question,feedback="",toast=""; string[] choices;
    string topic="Mixed"; string[] topics={"Mixed","Math","Science","Geography"};
    Color ink=new Color(.045f,.075f,.105f),mint=new Color(.58f,.97f,.67f),gold=new Color(1,.79f,.31f),pink=new Color(1,.39f,.48f);
    float Radius { get { return Mathf.Lerp(235,24,Mathf.Clamp01(elapsed/235)); } }
    int Alive { get { return fighters.Count(f=>f.hp>0); } }
    readonly string[] names={"Mochi","Pixel","Nimbus","Sprout","Cosmo","Noodle","Mango","Pebble","Orbit","Waffles","Echo","Tofu","Nova","Clover","Ziggy"};
    readonly string[][] trivia={
      new[]{"Science","Which planet is known as the Red Planet?","Mars","Venus","Jupiter","Neptune"},
      new[]{"Science","What gas do plants absorb during photosynthesis?","Carbon dioxide","Oxygen","Helium","Nitrogen"},
      new[]{"Science","What is the chemical formula for water?","H2O","CO2","O2","NaCl"},
      new[]{"Science","How many legs does an insect have?","6","4","8","10"},
      new[]{"Science","Which organ pumps blood around your body?","Heart","Lungs","Brain","Liver"},
      new[]{"Science","What force keeps planets in orbit?","Gravity","Friction","Magnetism","Sound"},
      new[]{"Science","Which state of matter has a fixed shape?","Solid","Liquid","Gas","Plasma"},
      new[]{"Science","What is the closest star to Earth?","The Sun","Sirius","Polaris","Vega"},
      new[]{"Science","What does a caterpillar become?","Butterfly","Dragonfly","Beetle","Spider"},
      new[]{"Science","Which part of a plant absorbs water?","Roots","Flowers","Leaves","Seeds"},
      new[]{"Geography","What is the capital of Japan?","Tokyo","Seoul","Beijing","Bangkok"},
      new[]{"Geography","Which is the largest ocean?","Pacific","Atlantic","Indian","Arctic"},
      new[]{"Geography","On which continent is Egypt?","Africa","Europe","South America","Australia"},
      new[]{"Geography","Which country is shaped like a boot?","Italy","Spain","Greece","Norway"},
      new[]{"Geography","What is the capital of France?","Paris","Rome","Berlin","Madrid"},
      new[]{"Geography","Which continent contains the South Pole?","Antarctica","Asia","Europe","Africa"},
      new[]{"Geography","Mount Everest is part of which mountain range?","Himalayas","Alps","Andes","Rockies"},
      new[]{"Geography","Which country contains the Great Barrier Reef?","Australia","Brazil","India","Canada"},
      new[]{"Geography","Which is the largest continent by area?","Asia","Africa","Europe","North America"},
      new[]{"Geography","Which country is home to the city of Toronto?","Canada","Mexico","France","Japan"}
    };
    void Start() {
      Application.targetFrameRate=60; best=PlayerPrefs.GetInt("best",0); font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
      dot=new Texture2D(32,32); for(int y=0;y<32;y++) for(int x=0;x<32;x++) dot.SetPixel(x,y,Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))<15?Color.white:Color.clear); dot.Apply();
      cam=new GameObject("Camera").AddComponent<Camera>(); cam.orthographic=true; cam.orthographicSize=24; cam.backgroundColor=new Color(.18f,.29f,.34f); cam.clearFlags=CameraClearFlags.SolidColor; cam.transform.rotation=Quaternion.Euler(58,0,0);
      RenderSettings.ambientLight=new Color(.78f,.85f,.91f); var sun=new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=.85f; sun.transform.rotation=Quaternion.Euler(55,-30,0); QualitySettings.shadows=ShadowQuality.Disable;
      NewArena(); InvokeRepeating("PublishState",.3f,.3f);
    }
    Material Mat(Color c) { if(!materials.ContainsKey(c)) { var m=new Material(Resources.Load<Shader>("Map")); m.color=c;  materials[c]=m; } return materials[c]; }
    GameObject Shape(PrimitiveType type,Vector3 p,Vector3 s,Color c,Transform parent=null) { var g=GameObject.CreatePrimitive(type); g.transform.position=p; g.transform.localScale=s; g.GetComponent<Renderer>().sharedMaterial=Mat(c); Destroy(g.GetComponent<Collider>()); if(parent)g.transform.SetParent(parent,true); return g; }
    void Ground(Vector3 p,Vector3 s,Color c) { world.Add(Shape(PrimitiveType.Cube,p,s,c)); }
    void NewArena() {
      foreach(var f in fighters)Destroy(f.body); fighters.Clear(); foreach(var g in world)Destroy(g);world.Clear();foreach(var c in caches)Destroy(c.body);caches.Clear();beacons.Clear();
      elapsed=0;ammo=24;transitionAt=0;quiz=false;map=false;paused=false;
      Random.InitState(7231+wave*97);
      Ground(new Vector3(0,-1,0),new Vector3(424,1.4f,424),new Color(.18f,.45f,.38f));
      // Nine large districts, connected by cross-map trails.
      for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++) {
        Color c=(x+z)%2==0?new Color(.27f,.52f,.39f):new Color(.25f,.48f,.43f);
        if(x==1&&z==1)c=new Color(.56f,.51f,.36f);if(x==-1&&z==1)c=new Color(.28f,.44f,.5f);
        Ground(new Vector3(x*140,-.22f,z*140),new Vector3(137,.2f,137),c);
        beacons.Add(new Vector3(x*132,0,z*132));
      }
      for(int i=-1;i<=1;i++){Ground(new Vector3(i*140,-.05f,0),new Vector3(8,.1f,420),new Color(.53f,.57f,.43f)); Ground(new Vector3(0,-.04f,i*140),new Vector3(420,.1f,8),new Color(.53f,.57f,.43f));}
      for(int i=0;i<360;i++) {
        Vector3 p=new Vector3(Random.Range(-202f,202f),0,Random.Range(-202f,202f));
        if(Mathf.Abs(p.x%140)<9||Mathf.Abs(p.z%140)<9||beacons.Any(b=>Vector3.Distance(p,b)<12))continue;
        if(i%4==0){var r=Shape(PrimitiveType.Cube,p+Vector3.up, new Vector3(3,2,3),new Color(.49f,.57f,.55f));r.transform.rotation=Quaternion.Euler(0,Random.Range(0,90),0);world.Add(r);}
        else {float h=Random.Range(3f,6f);world.Add(Shape(PrimitiveType.Cylinder,p+Vector3.up*h*.3f,new Vector3(.7f,h*.3f,.7f),new Color(.35f,.28f,.2f)));world.Add(Shape(PrimitiveType.Sphere,p+Vector3.up*h,new Vector3(h,h*1.2f,h), i%3==0?new Color(.22f,.57f,.4f):new Color(.12f,.37f,.32f)));}
      }
      foreach(var b in beacons){world.Add(Shape(PrimitiveType.Cylinder,b,new Vector3(11,.15f,11),new Color(.15f,.26f,.31f)));world.Add(Shape(PrimitiveType.Cube,b+Vector3.up*2,new Vector3(2.2f,4,2.2f),mint));world.Add(Shape(PrimitiveType.Sphere,b+Vector3.up*5,new Vector3(2,2,2),gold));}
      for(int i=0;i<70;i++){var p=new Vector3(Random.Range(-198f,198f),.7f,Random.Range(-198f,198f));var g=Shape(PrimitiveType.Cube,p,new Vector3(2,1.4f,1.5f),gold);caches.Add(new Cache{body=g});}
      // Ruins and a central arena give the enormous map distinct landmarks.
      foreach(var b in beacons)for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Vector3 p=b+new Vector3(Mathf.Cos(a)*16,2,Mathf.Sin(a)*16);world.Add(Shape(PrimitiveType.Cube,p,new Vector3(2,4,2),new Color(.6f,.64f,.55f)));}
      me=MakeFighter("YOU",new Vector3(7,.9f,-6),mint,true); fighters.Add(me);
      for(int i=0;i<15;i++){Vector3 p=i<3?new Vector3(25+i*14,.9f,Random.Range(16f,32f)):new Vector3(Random.Range(-180f,180f),.9f,Random.Range(-180f,180f)); fighters.Add(MakeFighter(names[i],p,Color.HSVToRGB(i/15f,.54f,.96f),false));}
      if(!ring){ring=new GameObject("Storm perimeter").AddComponent<LineRenderer>();ring.positionCount=161;ring.startWidth=1.4f;ring.endWidth=1.4f;ring.sharedMaterial=new Material(Resources.Load<Shader>("Map"));ring.startColor=pink;ring.endColor=pink;}
      cam.transform.position=me.P+new Vector3(0,36,-23); Toast("ARENA "+wave+"  /  Find energy. Outlast everyone.");
    }
    Fighter MakeFighter(string name,Vector3 p,Color c,bool player){
      var root=new GameObject(name);root.transform.position=p;
      Shape(PrimitiveType.Cube,p,new Vector3(1.8f,1.8f,1.6f),c,root.transform);
      Shape(PrimitiveType.Cube,p+new Vector3(-.44f,.25f,-.83f),new Vector3(.33f,.42f,.12f),ink,root.transform);
      Shape(PrimitiveType.Cube,p+new Vector3(.44f,.25f,-.83f),new Vector3(.33f,.42f,.12f),ink,root.transform);
      Shape(PrimitiveType.Cube,p+new Vector3(0,-.3f,-.84f),new Vector3(.55f,.12f,.12f),ink,root.transform);
      Shape(PrimitiveType.Cube,p+new Vector3(0,1.25f,0),new Vector3(.9f,.5f,.8f),player?gold:c,root.transform);
      return new Fighter{body=root,name=name,player=player,color=c,goal=p,fire=Time.time+3};
    }
    void Toast(string t){toast=t;toastAt=Time.time+4;}
    void Update(){
      if(!started)return;
      if(Input.GetKeyDown(KeyCode.Escape)&&!quiz)paused=!paused;
      if(Input.GetKeyDown(KeyCode.M)&&!quiz&&!paused)map=!map;
      if(transitionAt>0){if(Time.time>=transitionAt){wave++;NewArena();}return;}
      if(paused||map)return;
      if(quiz){if(!answered){for(int i=0;i<4;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i)))Choose(i);if(Time.time>quizEnd)Choose(-1);}else if(Time.time>quizAt){quiz=false;}return;}
      if(Input.GetKeyDown(KeyCode.Q)) { BeginQuiz();return; }
      elapsed+=Time.deltaTime;
      float h=Input.GetAxisRaw("Horizontal"),v=Input.GetAxisRaw("Vertical");Vector3 dir=new Vector3(h,0,v).normalized;
      float speed=Input.GetKey(KeyCode.LeftShift)?19:12;
      me.P+=dir*speed*Time.deltaTime;me.P=new Vector3(Mathf.Clamp(me.P.x,-207,207),.9f,Mathf.Clamp(me.P.z,-207,207));
      if(dir.sqrMagnitude>.01)me.body.transform.rotation=Quaternion.Slerp(me.body.transform.rotation,Quaternion.LookRotation(-dir),Time.deltaTime*14);
      cam.transform.position=Vector3.Lerp(cam.transform.position,me.P+new Vector3(0,36,-23),Time.deltaTime*8);
      if((Input.GetKey(KeyCode.Space)||Input.GetMouseButton(0))&&Time.time>shotAt){
        var target=fighters.Where(f=>!f.player&&f.hp>0).OrderBy(f=>Vector3.Distance(me.P,f.P)).FirstOrDefault();
        if(target!=null&&Vector3.Distance(me.P,target.P)<27&&ammo>0){ammo--;shotAt=Time.time+.24f;Shoot(me,target,26,mint);}else if(ammo==0&&Time.time>shotAt){Toast("Out of energy! Press Q to answer a question.");shotAt=Time.time+2;}
      }
      foreach(var c in caches)if(!c.used&&Vector3.Distance(me.P,c.body.transform.position)<3){c.used=true;c.body.SetActive(false);ammo+=10;me.hp=Mathf.Min(100,me.hp+20);Toast("SUPPLY FOUND   +10 energy / +20 health");}
      foreach(var f in fighters){
        if(f.hp<=0)continue;
        if(new Vector2(f.P.x,f.P.z).magnitude>Radius)Hurt(f,Time.deltaTime*(elapsed>230?14:6),null);
        if(f.player||f.hp<=0)continue;
        var target=fighters.Where(o=>o!=f&&o.hp>0).OrderBy(o=>Vector3.SqrMagnitude(o.P-f.P)).FirstOrDefault();
        if(target==null)continue;float d=Vector3.Distance(f.P,target.P);
        if(Time.time>f.think){f.think=Time.time+Random.Range(1f,3f);f.goal=new Vector3(Random.Range(-Radius*.6f,Radius*.6f),.9f,Random.Range(-Radius*.6f,Radius*.6f));}
        Vector3 dest=d<55?target.P:f.goal;
        if(new Vector2(f.P.x,f.P.z).magnitude>Radius-8)dest=Vector3.zero;
        Vector3 move=(dest-f.P);move.y=0;
        if(d>12||dest==Vector3.zero) f.P+=move.normalized*(6+Mathf.Min(wave,10)*.25f)*Time.deltaTime;
        if(move.sqrMagnitude>.1f)f.body.transform.rotation=Quaternion.LookRotation(-move);
        if(d<19&&Time.time>f.fire){f.fire=Time.time+Random.Range(1f,2f);if(Random.value<.65f)Shoot(f,target,8+Mathf.Min(wave,8),pink);}
      }
      for(int i=0;i<=160;i++){float a=i/160f*Mathf.PI*2;ring.SetPosition(i,new Vector3(Mathf.Cos(a)*Radius,.8f,Mathf.Sin(a)*Radius));}
      if(me.hp<=0)Finish(false);else if(Alive==1)Finish(true);
    }
    void Shoot(Fighter from,Fighter to,float damage,Color c){
      var g=new GameObject("Energy bolt");var l=g.AddComponent<LineRenderer>();l.positionCount=2;l.SetPosition(0,from.P);l.SetPosition(1,to.P);l.startWidth=.22f;l.endWidth=.08f;l.sharedMaterial=ring.sharedMaterial;l.startColor=c;l.endColor=c;Destroy(g,.12f);Hurt(to,damage,from);
    }
    void Hurt(Fighter f,float amount,Fighter from){if(f.hp<=0)return;f.hp-=amount;if(f.hp<=0){f.hp=0;f.body.SetActive(false);if(from==me){kills++;ammo+=6;me.hp=Mathf.Min(100,me.hp+12);Toast("ELIMINATED "+f.name+"   +6 energy / +12 health");}}}
    void Finish(bool won){if(transitionAt>0)return;if(won){crowns++;streak++;best=Mathf.Max(best,streak);PlayerPrefs.SetInt("best",best);PlayerPrefs.Save();}else streak=0;transitionAt=Time.time+7;}
    void BeginQuiz(){
      quiz=true;answered=false;selected=-1;total++;choices=new string[4];
      bool math=topic=="Math"||(topic=="Mixed"&&Random.value<.55f);
      if(math){int a=Random.Range(2,12+Mathf.Min(wave,12)*2),b=Random.Range(2,12);int kind=Random.Range(0,3);int val=kind==0?a+b:kind==1?a*b:a+b;question=kind==0?a+" + "+b+" = ?":kind==1?a+" × "+b+" = ?":"? − "+b+" = "+a;var nums=new HashSet<int>{val};while(nums.Count<4)nums.Add(Mathf.Max(0,val+Random.Range(-12,13)));choices=nums.Select(n=>n.ToString()).OrderBy(n=>Random.value).ToArray();answer=System.Array.IndexOf(choices,val.ToString());}
      else{var pool=trivia.Where(t=>topic=="Mixed"||t[0]==topic).ToArray();var q=pool[Random.Range(0,pool.Length)];question=q[1];choices=q.Skip(2).OrderBy(n=>Random.value).ToArray();answer=System.Array.IndexOf(choices,q[2]);}
      quizEnd=Time.time+15;
    }
    void Choose(int n){if(answered)return;selected=n;answered=true;bool right=n==answer;bool near=beacons.Any(b=>Vector3.Distance(b,me.P)<12);if(right){correct++;ammo+=near?28:16;me.hp=Mathf.Min(100,me.hp+(near?30:12));feedback="CORRECT!  +"+(near?28:16)+" energy  /  +"+(near?30:12)+" health";}else feedback=(n<0?"TIME UP. ":"NOT QUITE. ")+"Answer: "+choices[answer];quizAt=Time.time+1.8f;}
    void Box(float x,float y,float w,float h,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=Color.white;}
    void Circle(float x,float y,float size,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x-size/2,y-size/2,size,size),dot);GUI.color=Color.white;}
    void Text(float x,float y,float w,float h,string t,int size,Color c,TextAnchor align=TextAnchor.UpperLeft){var s=new GUIStyle(GUI.skin.label){font=font,fontSize=size,alignment=align,wordWrap=true};s.normal.textColor=c;GUI.Label(new Rect(x,y,w,h),t,s);}
    bool Button(float x,float y,float w,float h,string t,Color c){Box(x,y,w,h,c);Text(x+8,y,w-16,h,t,18,ink,TextAnchor.MiddleCenter);return GUI.Button(new Rect(x,y,w,h),"",GUIStyle.none);}
    void Mini(float x,float y,float s,bool large){
      Box(x-2,y-2,s+4,s+4,new Color(.5f,.65f,.59f));Box(x,y,s,s,new Color(.1f,.23f,.22f));
      for(int a=1;a<3;a++){Box(x+a*s/3,y,1,s,new Color(.21f,.36f,.31f));Box(x,y+a*s/3,s,1,new Color(.21f,.36f,.31f));}
      float r=Radius/424*s;for(int i=0;i<100;i++){float a=i/100f*Mathf.PI*2;Circle(x+s/2+Mathf.Cos(a)*r,y+s/2+Mathf.Sin(a)*r,large?3:2,pink);}
      foreach(var b in beacons)Circle(x+(b.x+212)/424*s,y+(212-b.z)/424*s,large?9:4,gold);
      foreach(var f in fighters)if(f.hp>0)Circle(x+(f.P.x+212)/424*s,y+(212-f.P.z)/424*s,f.player?(large?13:8):(large?8:4),f.player?mint:pink);
      if(large){string[] labels={"FROST GROVE","NORTHWATCH","SUNLIT RUINS","MOSS WOODS","CROWN CROSSING","AMBER FIELDS","WILD GARDEN","SOUTH CAMP","STONE HOLLOW"};for(int i=0;i<9;i++)Text(x+(i%3)*s/3,y+(i/3)*s/3+25,s/3,40,labels[i],13,new Color(.65f,.78f,.7f),TextAnchor.UpperCenter);}
    }
    void OnGUI(){
      GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/800f,1));
      if(!started){Menu();return;}
      Box(0,0,1280,82,new Color(.035f,.065f,.09f,.96f));Text(25,14,230,35,"QUIZ / ROYALE",25,mint);Text(27,49,240,25,"THE ENDLESS EXPEDITION",11,new Color(.59f,.69f,.7f));
      Text(350,12,160,26,"ARENA "+wave,16,gold);Text(350,42,150,28,Alive+" / 16 ALIVE",20,Color.white);
      Text(545,12,180,26,"CROWN STREAK",12,new Color(.59f,.69f,.7f));Text(545,37,170,32,streak+"   /   BEST "+best,21,Color.white);
      Text(768,12,170,26,"ELIMINATIONS",12,new Color(.59f,.69f,.7f));Text(768,37,170,32,kills.ToString("00"),23,Color.white);
      if(Button(1060,20,100,42,"M  MAP",mint))map=!map;if(Button(1170,20,85,42,"PAUSE",gold))paused=!paused;
      Mini(25,105,173,false);Text(25,285,220,28,"424 × 424 WORLD",12,Color.white);
      Text(25,311,225,30,"STORM  "+Mathf.CeilToInt(Radius)+"m",15,pink);
      var nearest=fighters.Where(f=>!f.player&&f.hp>0).OrderBy(f=>Vector3.Distance(me.P,f.P)).FirstOrDefault();
      if(nearest!=null){Vector3 d=nearest.P-me.P;string compass=(d.z>12?"N":"")+(d.z< -12?"S":"")+(d.x>12?"E":"")+(d.x< -12?"W":"");Text(25,345,230,50,"NEAREST RIVAL\n"+Mathf.RoundToInt(d.magnitude)+"m "+compass,14,Color.white);}
      Text(1015,110,230,26,"FIELD GUIDE",12,gold);Text(1015,142,235,180,"WASD / arrows   Move\nSHIFT   Sprint\nSPACE / hold click   Fire\nQ   Answer for energy\n1–4   Choose answer\nM   Explore world map\nESC   Pause",14,Color.white);
      Text(1015,336,230,100,"Gold caches restore supplies.\nMint beacons double quiz rewards.\nStay inside the pink storm ring.",13,new Color(.71f,.82f,.78f));
      foreach(var f in fighters)if(f.hp>0){var p=cam.WorldToViewportPoint(f.P+Vector3.up*2);if(p.z>0&&p.x>.18&&p.x<.78&&p.y>.17&&p.y<.84){float x=p.x*1280,y=(1-p.y)*800;Text(x-55,y-34,110,25,f.name,13,f.player?mint:Color.white,TextAnchor.MiddleCenter);Box(x-23,y-9,46,4,ink);Box(x-23,y-9,46*f.hp/100,4,f.player?mint:pink);}}
      Box(280,698,720,82,ink);Text(302,711,150,24,"HEALTH   "+Mathf.CeilToInt(me.hp),13,Color.white);Box(302,747,180,9,new Color(.2f,.28f,.3f));Box(302,747,180*me.hp/100,9,mint);
      Text(520,711,190,25,"ENERGY",12,gold);Text(520,734,180,34,ammo+"  /  shots",24,Color.white);
      if(Button(747,717,232,46,"Q   QUIZ TO RECHARGE",mint)&&!quiz&&!map&&!paused&&transitionAt==0)BeginQuiz();
      if(Time.time<toastAt&&!quiz&&transitionAt==0){Box(300,105,680,46,ink);Text(310,105,660,46,toast,16,mint,TextAnchor.MiddleCenter);}
      if(quiz)QuizPanel();else if(transitionAt>0){Overlay();Text(220,215,840,65,me.hp>0?"CROWN SECURED.":"BACK INTO THE WILD.",46,me.hp>0?gold:mint,TextAnchor.MiddleCenter);Text(270,300,740,80,me.hp>0?"You outlasted all 15 rivals. Another arena awaits.":"Every expedition is a fresh start. Your next arena awaits.",22,Color.white,TextAnchor.MiddleCenter);Text(300,410,680,60,"ARENA "+(wave+1)+" IN "+Mathf.Max(1,Mathf.CeilToInt(transitionAt-Time.time))+"  /  THE GAME NEVER ENDS",18,mint,TextAnchor.MiddleCenter);}
      else if(map){Overlay();Text(280,92,720,40,"THE WILDS  /  LIVE WORLD MAP",26,mint,TextAnchor.MiddleCenter);Mini(380,155,500,true);Text(250,678,780,35,"MINT: YOU    /    CORAL: RIVALS    /    GOLD: QUIZ BEACONS",14,Color.white,TextAnchor.MiddleCenter);if(Button(490,730,300,45,"M   RETURN TO EXPLORING",mint))map=false;}
      else if(paused){Overlay();Text(390,240,500,70,"TAKE A BREATHER.",38,mint,TextAnchor.MiddleCenter);Text(390,325,500,50,"The arena is paused.",20,Color.white,TextAnchor.MiddleCenter);if(Button(490,430,300,55,"RESUME EXPEDITION",mint))paused=false;}
    }
    void Overlay(){Box(0,82,1280,718,new Color(.025f,.05f,.075f,.95f));}
    void Menu(){
      Box(0,0,1280,800,new Color(.025f,.055f,.07f,.85f));Text(80,56,500,30,"A NEVER-ENDING QUIZ ADVENTURE",14,mint);
      Text(76,117,740,190,"QUIZ\nROYALE.",84,Color.white);Text(82,335,575,90,"One explorer. Fifteen rivals.\nA whole world to outsmart.",27,new Color(.79f,.89f,.84f));
      Text(82,453,550,80,"Explore a huge 3D map, answer to power up, and battle for the crown. Win or lose, the next arena is always waiting.",18,new Color(.68f,.8f,.77f));
      Text(82,555,400,25,"CHOOSE YOUR QUESTION MIX",12,gold);for(int i=0;i<4;i++)if(Button(82+i*133,590,122,42,topics[i],topic==topics[i]?mint:new Color(.48f,.59f,.58f)))topic=topics[i];
      if(Button(82,665,520,65,"ENTER THE WILDS     →",mint)){started=true;Toast("WASD to explore / SPACE to fire / Q to recharge");}
      Box(815,106,365,570,ink);Text(842,132,310,35,"YOUR EXPEDITION",16,gold);Mini(850,197,290,false);Text(845,512,305,80,"9 DISTRICTS  /  15 BOTS\n424 × 424 MAP  /  ENDLESS ARENAS",16,Color.white);Text(845,610,300,40,"Best crown streak   "+best,17,mint);
      Text(82,756,1100,25,"DESKTOP BROWSER  •  WASD MOVE  •  SHIFT SPRINT  •  SPACE FIRE  •  Q QUIZ  •  M MAP",12,new Color(.6f,.7f,.68f));
    }
    void QuizPanel(){Overlay();Text(290,145,700,30,"ENERGY CHALLENGE  /  "+topic.ToUpper(),14,gold,TextAnchor.MiddleCenter);Text(280,202,720,100,question,32,Color.white,TextAnchor.MiddleCenter);Text(300,305,680,30,answered?feedback:Mathf.CeilToInt(Mathf.Max(0,quizEnd-Time.time))+" SECONDS  /  THE WORLD IS PAUSED",16,mint,TextAnchor.MiddleCenter);for(int i=0;i<4;i++){float x=290+(i%2)*360,y=380+(i/2)*94;Color c=answered?(i==answer?mint:i==selected?pink:new Color(.4f,.5f,.5f)):new Color(.74f,.85f,.8f);if(Button(x,y,340,75,(i+1)+"    "+choices[i],c)&&!answered)Choose(i);}Text(310,625,660,70,"Correct answers restore energy and health.\nAnswer near a mint beacon for a larger supply boost.",16,Color.white,TextAnchor.MiddleCenter);}
}

