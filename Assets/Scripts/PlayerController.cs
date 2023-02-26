using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody body;
    private CapsuleCollider collider;
    private Camera camera;

    [SerializeField]
    private bool playMode = true;


    [SerializeField]
    private float gravity = 0.5f;

    [SerializeField]
    private Vector3 friction = new Vector3(0.25f,0,0.25f);
    [SerializeField]
    private Vector3 waterDrag = new Vector3(0.1f,0.1f,0.1f);
    [SerializeField]
    private float deadZone;

    [SerializeField]
    private float walkSpeed = 0.5f;
    [SerializeField]
    private float jumpPower = 1f;
    [SerializeField]
    private float swimPower = 0.25f;
    [SerializeField]
    private float swimWeight = 0.35f;
    [SerializeField]
    private bool canSwim = true;

    [SerializeField]
    private Vector2 cameraAngle;
    [SerializeField]
    private Vector2 cameraSpeed;

    [SerializeField]
    private float eyeHeight = 0.75f;
    [SerializeField]
    private float crouchEyeHeight = -0.25f;
    private float _cameraHeight;
    private float _crouch;
    [SerializeField]
    private float crouchWalk = 0.5f;

    private bool _onGround;

    private bool _inWater;
    private bool _underWater;
    private float _waterSurface;
    private float _cameraWaterSurface;

    private int _climbCount;
    private bool _onClimb;

    [SerializeField]
    private float climbPower = 0.2f;
    [SerializeField]
    private float climbFriction = 0.25f;

    [SerializeField]
    private AudioSource jump;
    [SerializeField]
    private AudioSource land;
    [SerializeField]
    private AudioSource splash_in;
    [SerializeField]
    private AudioSource splash_out;
    [SerializeField]
    private AudioSource submerge;

    [SerializeField]
    private Material underwaterFilter;

    private Vector3 _saveVelocity;


    void Start()
    {
        body = GetComponent<Rigidbody>();
        collider = GetComponent<CapsuleCollider>();
        camera = GetComponentInChildren<Camera>();
        //Debug.Log(collider.center);

        _onGround = false;

        _cameraHeight=eyeHeight;
        _crouch = 0;

        _inWater = false;
        _underWater = false;
        _waterSurface = 0;

        underwaterFilter.SetFloat("_WaterHeight",-1);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    void FixedUpdate()
    {
        
        crouching();
        moveBody();
        moveCamera();

        //getGroundCollision();
        //getWaterCollision();
    }

    void Update(){
        getGroundCollision();
        getWaterCollision();
    }

    private void getGroundCollision(){
        if (Physics.SphereCast(transform.position,transform.lossyScale.x*0.45f,new Vector3(0,-1,0),out RaycastHit hit,transform.lossyScale.y*0.65f,LayerMask.GetMask("Default"),QueryTriggerInteraction.Ignore))
        {
            if(!_onGround){
                land.volume = Mathf.Abs(_saveVelocity.y/transform.localScale.y)*0.035f;
                land.Play();
            }
            _onGround = true;
        }
        else
        {
            _onGround = false;
        }
        _saveVelocity=body.velocity;
    }
    
    private void getWaterCollision(){
        RaycastHit[] hits = Physics.RaycastAll(transform.position+new Vector3(0,-1,0)*transform.lossyScale.y,new Vector3(0,1,0),Mathf.Infinity,LayerMask.GetMask("Water"));
        if(hits.Length%2==1){
            if(!_inWater){
                enterWater();
            }
            _inWater=true;
            float waterY = Mathf.Infinity;
            for(int i = 0;i < hits.Length;i ++){
                if(hits[i].point.y<waterY){
                    waterY=hits[i].point.y;
                }
            }
            setWaterSurface(waterY);
        } else {
            if(_inWater){
                exitWater();
            }
            _inWater=false;
            setWaterSurface(transform.position.y-transform.lossyScale.y);
        }

        if(_cameraWaterSurface>=0.5f){
            if(!_underWater){
                enterUnderwater();
            }
            _underWater=true;
        } else {
            if(_underWater){
                exitUnderwater();
            }
            _underWater=false;
        }
        //Debug.Log(_inWater);
    }
    private void moveBody(){
        Vector3 velocity = body.velocity;

        velocity = new  Vector3(velocity.x*(1-friction.x),velocity.y*(1-friction.y),velocity.z*(1-friction.z));
        velocity.y-=gravity;

        velocity=locomotion(velocity);
        velocity=waterMotion(velocity);
        
        body.velocity=velocity;
    }

    private void crouching(){
        if(playMode){
            if(_onGround&&Input.GetKey("left shift")||Input.GetKey("right shift")){
                _crouch += (1-_crouch)*0.2f;
            } else {
                _crouch += (0-_crouch)*0.2f;
            }
        }
        float targetHeight = 2-_crouch;
        if(collider.height>targetHeight){
            collider.height=targetHeight;
            collider.center=new Vector3(collider.center.x,_crouch*-0.5f,collider.center.z);
        }
        if(collider.height<targetHeight){
            if(Physics.SphereCast(transform.position+new Vector3(0,-0.5f,0)*transform.lossyScale.y,transform.lossyScale.x*0.5f,new Vector3(0,1,0),out RaycastHit hit,1-_crouch*transform.lossyScale.y,~LayerMask.GetMask("Water"),QueryTriggerInteraction.Ignore)){
                _crouch = 1-(hit.distance/transform.lossyScale.y);
                collider.height=2-_crouch;
                collider.center=new Vector3(collider.center.x,_crouch*-0.5f,collider.center.z);
            } else {
                collider.height=targetHeight;
                collider.center=new Vector3(collider.center.x,_crouch*-0.5f,collider.center.z);
            }
        }
        //Debug.Log(_crouch);
    }

    private Vector3 locomotion(Vector3 velocity){
        Vector2 motion = new Vector2(0,0);
        if(playMode){
            if (Input.GetKey("up") || Input.GetKey("w"))
            {
                motion.y += 0.5f;
            }
            if (Input.GetKey("down") || Input.GetKey("s"))
            {
                motion.y -= 0.5f;
            }
            if (Input.GetKey("right") || Input.GetKey("d"))
            {
                motion.x += 0.5f;
            }
            if (Input.GetKey("left") || Input.GetKey("a"))
            {
                motion.x -= 0.5f;
            }
        }
        if(motion.magnitude>0){
            float walkAngle = Mathf.Atan2(-motion.y,motion.x);
            float speed = walkSpeed*(1-_crouch*(1-crouchWalk));
            velocity.x+=Mathf.Cos(walkAngle+(cameraAngle.y*(Mathf.PI/180f)))*speed;
            velocity.z-=Mathf.Sin(walkAngle+(cameraAngle.y*(Mathf.PI/180f)))*speed;
        } else {
            if(_onGround&&!(Input.GetKey("space")&&playMode)){
                velocity.y=0;
                if(velocity.magnitude<deadZone){
                    velocity = new Vector3(0,0,0);
                }
            }
        }

        if(playMode&&Input.GetKey("space")){
            if(_onGround){
                if(velocity.y<0){
                    velocity.y=0;
                }
                float jp = jumpPower;
                if(_inWater){
                    jp*=0.5f;
                }
                if(velocity.y<jp*0.5f){
                    velocity.y+=jp;
                    if(!jump.isPlaying){
                        jump.Play();
                    }
                } else {
                    if(velocity.y<jumpPower){
                        velocity.y=jumpPower;
                        if(!jump.isPlaying){
                            jump.Play();
                        }
                    }
                }
            } else {
                if(_inWater&&canSwim){
                    velocity.y += jumpPower*swimPower*_waterSurface;
                }
                if(_onClimb){
                    velocity.y += jumpPower*climbPower;
                }
            }
        }
        if(_onClimb){
            velocity*=(1-climbFriction);
        }
        return velocity;
    }

    private Vector3 waterMotion(Vector3 velocity){
        if(_inWater){
            float yv = velocity.y;
            if(yv<0){
                yv*=(1-waterDrag.y);
            }
            if(yv>-gravity){
                yv*=(1-(waterDrag.y*0.5f));
            }
            if(playMode&&(canSwim&&!_onGround&&Input.GetKey("left shift")||Input.GetKey("right shift"))){
                yv-=jumpPower*swimPower;
            }
            velocity = new Vector3(velocity.x*(1-waterDrag.x),yv,velocity.z*(1-waterDrag.z)); 
        }
        return velocity;
    }

    private void moveCamera(){

        if(playMode){
            cameraAngle.x += Input.GetAxis("Mouse Y")*cameraSpeed.x;
            cameraAngle.y += Input.GetAxis("Mouse X")*cameraSpeed.y;
        }

        _cameraHeight = _crouch*crouchEyeHeight + (1-_crouch)*eyeHeight;

        cameraAngle.x = Mathf.Clamp(cameraAngle.x,-90,90);
        camera.gameObject.transform.eulerAngles = new Vector3(cameraAngle.x,cameraAngle.y,0);
        camera.gameObject.transform.localPosition = new Vector3(0,_cameraHeight,0);
    }

    public float getCameraHeight(){return _cameraHeight*transform.localScale.y;}

    public void setWaterSurface(float planeY){
        float cam_height = camera.nearClipPlane * Mathf.Tan(camera.fieldOfView*(Mathf.PI/360f)) * 2f;
        float cam_angle = (90-camera.gameObject.transform.eulerAngles.x)*(Mathf.PI/180f);
        float cam_y = ((((transform.position.y + getCameraHeight())-planeY)/Mathf.Cos(cam_angle))-camera.nearClipPlane)/Mathf.Tan(cam_angle);
        _cameraWaterSurface = Mathf.Clamp(0.5f-(cam_y/cam_height),-1,2);

        _waterSurface = Mathf.Clamp(1-(swimWeight*((transform.position.y + getCameraHeight())-planeY)/getCameraHeight()),0,1);

        underwaterFilter.SetFloat("_WaterHeight",_cameraWaterSurface);
        //Debug.Log(_cameraWaterSurface);
    }
    private void enterWater(){
        if(!splash_in.isPlaying){
            splash_in.volume = Mathf.Abs(_saveVelocity.y/transform.localScale.y)*0.02f;
            splash_in.Play();
        }
    }
    private void exitWater(){
        if(!splash_out.isPlaying){
            splash_out.Play();
        }
    }
    private void enterUnderwater(){
        if(!submerge.isPlaying){
            submerge.volume = 0.5f+Mathf.Abs(_saveVelocity.y/transform.localScale.y)*0.02f;
            submerge.Play();
        }
    }
    private void exitUnderwater(){
        if(!splash_out.isPlaying){
            splash_out.Play();
        }
    }

    public void setClimb(bool c){
        if(c){
            _climbCount++;
        } else {
            _climbCount--;
        }
        _onClimb = (_climbCount>0);
    }

    public void disableSwim(){
        canSwim=false;
    }
    public void platformMotion(Vector3 v){
        if(_onGround){
            transform.position = transform.position+v;
        }
    }
    public void pointCamera(Vector3 v){
        cameraAngle=v;
    }
}
