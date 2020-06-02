using UnityEngine;
using System.Collections;

public class UiPlacement : MonoBehaviour {

	public enum ElementAnchor {
		//bits 2 and 3 show top (10) middle (00) bottom (01)
		//bits 0 and 1 show left (10) center (00) right (01) 
		center = 0x0,
		top = 0x8,
		bottom = 0x4,
		left = 0x2,
		right = 0x1,
		topleft = top | left,
		topright = top | right,
		bottomleft = bottom | left,
		bottomright = bottom | right
	}

	public enum AspectComparator {
		sameRatio = 0,
		atLeastAsWide = 1,
		wider = 2,
		//atLeastAsNarrow = -2,
		//Narrower = -1,
		atLeastAsTall = -2,
		taller = -1,
		//atLeastAsShort = 1,
		//shorter = 2
	}

	[System.Serializable]
	public class ScreenPlacement {
		public Vector2 ScreenPosition;
		public Vector2 ScreenSize;
		public Vector2 ScreenSubtraction;
		public ElementAnchor Anchor;
	}

	[System.Serializable]
	public class PlacementCondition {
		public Vector2 AspectRatio;
		public AspectComparator Comparator;
		public ScreenPlacement Placement;
	}

	public PlacementCondition[] ConditionalPlacements;
	public ScreenPlacement DefaultPlacement;

	public Camera CameraOverride;

	public bool OverrideBounds = false;
	public Vector2 SizeOverride;
	public Vector2 CenterOverride;

	public bool UpdateInEdit = false;

	private Camera cam;
	private Vector3 originalScale;
	private Bounds bounds;

	// Use this for initialization
	void Start () {
		SetPlacement();
	}

	void OnValidate () {
		if(UpdateInEdit && gameObject.activeInHierarchy && !Application.isPlaying){
			SetPlacement();
		}
	}

	private void SetPlacement(){
		if(CameraOverride == null){
			cam = Camera.main;
		} else {
			cam = CameraOverride;
		}

		if(cam == null){
			return;
		}

		if(!cam.orthographic){
			throw new System.NotImplementedException("UI Placement system is not set up to handle a perspective camera");
		}

		bounds = new Bounds();
		if(originalScale == Vector3.zero) {
			originalScale = transform.localScale;
		} else {
			transform.localScale = originalScale;
		}
		//get the size of the component
		if(OverrideBounds){
			Vector2 adjustedSizeOverride = SizeOverride;
			adjustedSizeOverride.Scale(transform.localScale);
			bounds = new Bounds(transform.TransformPoint(CenterOverride), adjustedSizeOverride);
		} else {
			foreach(Renderer rend in GetComponentsInChildren<Renderer>()){
				if(bounds.size == Vector3.zero){
					bounds = rend.bounds;
				} else {
					bounds.Encapsulate(rend.bounds);
				}
			}
		}
		//if the bounds are still undefined, put in some dummy values
		if(bounds.size == Vector3.zero){
			bounds = new Bounds(transform.position, Vector2.one);
		}

		ScreenPlacement sp = FindTargetPlacement();
		Transform trans = transform;

		//adjust the CameraSize based on any aspect subtraction that we're using

		float subtractionAspect;
		if(sp.ScreenSubtraction.y > 0){
			subtractionAspect = sp.ScreenSubtraction.x / sp.ScreenSubtraction.y;
		} else {
			subtractionAspect = 0;
		}
			
		float adjustedAspect, adjustedSize;
		if(cam.aspect > subtractionAspect){
			//the screen is wider than the subtraction region, remove width
			adjustedAspect = cam.aspect - subtractionAspect;
			adjustedSize = cam.orthographicSize;
		} else {
			adjustedAspect = subtractionAspect - cam.aspect;
			adjustedSize = (cam.orthographicSize * 2 * cam.aspect) / adjustedAspect;
		}
		Vector2 CameraSize = new Vector2(adjustedSize * 2 * adjustedAspect, cam.orthographicSize * 2);

		Vector2 scale = Vector2.one;

		//scale the object based on screen size
		if(sp.ScreenSize.x != 0 || sp.ScreenSize.y != 0){
			if(sp.ScreenSize.x != 0){
				if(sp.ScreenSize.y != 0){
					//scale x and y based on total screen size
					scale.x = (CameraSize.x * sp.ScreenSize.x) / bounds.size.x;
					scale.y = (CameraSize.y * sp.ScreenSize.y) / bounds.size.y;
				} else {
					//uniform scale to make the width a portion of the screen
					scale *= (CameraSize.x * sp.ScreenSize.x) / bounds.size.x;
				}
			} else {
				//uniform scale to make the height a portion of the screen
				scale *= (CameraSize.y * sp.ScreenSize.y) / bounds.size.y;
			}
		}

		//get the local position of the anchor
		Vector2 localAnchor = trans.InverseTransformPoint(GetWorldAnchorPoint(bounds, sp.Anchor));
		//take the new scale into account
		localAnchor.Scale(scale);
		//transform back into world space
		Vector2 worldAnchor = trans.TransformPoint(localAnchor);

		//move the object so that the anchor lines up with the screen position
		Vector2 targetPosition = cam.ViewportToWorldPoint(sp.ScreenPosition);
		if((Vector2)trans.position != targetPosition - worldAnchor){
			trans.position += (Vector3)(targetPosition - worldAnchor);
		}
		//apply the scale to the object
		if(scale != Vector2.one){
			Vector3 localScale = trans.localScale;
			localScale.Scale(scale);
			trans.localScale = (Vector3)localScale + Vector3.forward;
		}
	}

	//find the appropriate conditional placement to use based on aspect ratio
	//will return the first conditional placement that returns true, or the default
	private ScreenPlacement FindTargetPlacement(){
		foreach(PlacementCondition pc in ConditionalPlacements){
			//throw out any aspect ratios with non-positive values
			if(pc.AspectRatio.x <= 0 || pc.AspectRatio.y <= 0){
				continue;
			}
			if(checkAspectComparator(cam.aspect, pc.AspectRatio.x / pc.AspectRatio.y, pc.Comparator)){
				return pc.Placement;
			}
		}
		return DefaultPlacement;
	}

	private bool checkAspectComparator(float curAspect, float compAspect, AspectComparator comparator){
		//check for the reverse test
		if((int)comparator < 0){
			return checkAspectComparator(compAspect, curAspect, (AspectComparator)((int)comparator * -1));
		}

		//check the different comparison types
		switch(comparator){
		case AspectComparator.sameRatio:
			return curAspect == compAspect;
		case AspectComparator.atLeastAsWide:
			return curAspect >= compAspect;
		case AspectComparator.wider:
		default:
			return curAspect > compAspect;
		}
	}

	private Vector2 GetWorldAnchorPoint(Bounds bb, ElementAnchor anchor){
		Vector2 ret = bb.center;
		if(((uint)anchor & (uint)ElementAnchor.top) > 0){
			ret.y += bb.extents.y;
		} else if (((uint)anchor & (uint)ElementAnchor.bottom) > 0){
			ret.y -= bb.extents.y;
		}

		if(((uint)anchor & (uint)ElementAnchor.right) > 0){
			ret.x += bb.extents.x;
		} else if (((uint)anchor & (uint)ElementAnchor.left) > 0){
			ret.x -= bb.extents.x;
		}
		return ret;
	}

}
