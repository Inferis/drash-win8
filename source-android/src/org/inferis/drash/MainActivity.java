package org.inferis.drash;

import android.os.Bundle;
import android.annotation.SuppressLint;
import android.app.Activity;
import android.util.Log;
import android.view.Menu;
import android.view.MotionEvent;
import android.view.View;
import android.widget.RelativeLayout;
import android.widget.LinearLayout;
import android.widget.ImageView;
import android.view.animation.*;
import android.view.animation.Animation.AnimationListener;

@SuppressLint("NewApi")
public class MainActivity extends Activity implements View.OnTouchListener {
	private boolean intensityValueShown;
	
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        
        intensityValueShown = false;

        // disable hardware rendering for ICS so that the actual dotted line is shown
        disableHardwareRendering(findViewById(R.id.dotted_top));
        disableHardwareRendering(findViewById(R.id.dotted_bottom));
        
        // add touch events
        RelativeLayout intensityLayout = (RelativeLayout)findViewById(R.id.intensityLayout);
        intensityLayout.setOnTouchListener(this);
    }

    public boolean onTouch(View v, MotionEvent event) {
    	if (event.getAction() != MotionEvent.ACTION_UP) return true;
    	Log.v("DRASH", "action = " + event.getAction() + ", shown = " + intensityValueShown);
    	
    	final View intensityImage = findViewById(R.id.intensityImage);
    	final View intensityValue = findViewById(R.id.intensityValue);
    	
    	if (intensityValueShown) {
    		fadeOut(intensityValue, new Completion() {
				public void onComplete() {
		    		fadeIn(intensityImage, null);
				}
			});
    	}
    	else {
    		fadeOut(intensityImage, new Completion() {
				public void onComplete() {
		    		fadeIn(intensityValue, null);
				}
			});
    	}
    	

    	intensityValueShown = !intensityValueShown;
    	return true;
    }
    
    private void fadeIn(final View view, final Completion completion) {
		Animation anim = AnimationUtils.loadAnimation(this, R.anim.fade_in);
		anim.setFillAfter(true);
		anim.setAnimationListener(new AnimationListener() {
			public void onAnimationStart(Animation animation) {}
			public void onAnimationRepeat(Animation animation) {}
			public void onAnimationEnd(Animation animation) {
				if (completion != null)	completion.onComplete();
			}
		});
    	Log.v("DRASH", "fadein " + view.getId());
		view.startAnimation(anim);
    }
    
    private void fadeOut(final View view, final Completion completion) {
		Animation anim = AnimationUtils.loadAnimation(this, R.anim.fade_out);
		anim.setFillAfter(true);
		anim.setAnimationListener(new AnimationListener() {
			public void onAnimationStart(Animation animation) {}
			public void onAnimationRepeat(Animation animation) {}
			public void onAnimationEnd(Animation animation) {
				if (completion != null)	completion.onComplete();
			}
		});
    	Log.v("DRASH", "fadeout " + view.getId());
		view.startAnimation(anim);
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        getMenuInflater().inflate(R.menu.activity_main, menu);
        return true;
    }
    
    public void disableHardwareRendering(View v) {
        if(android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.HONEYCOMB) {
            v.setLayerType(View.LAYER_TYPE_SOFTWARE, null);
        } 
   }
}
