package vn.vhdchy.app;

import android.app.Activity;
import android.os.Bundle;
import android.view.Gravity;
import android.view.ViewGroup;
import android.widget.LinearLayout;
import android.widget.TextView;

public final class MainActivity extends Activity {
    private static final String DOMAIN_CONTRACT = "VHDCHY_DOMAIN_V1";
    private static final String SLICE = "IDENTITY_EMPLOYEE_ATTENDANCE";

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setGravity(Gravity.CENTER_HORIZONTAL);
        int padding = dp(24);
        root.setPadding(padding, padding, padding, padding);

        TextView title = text("VẬN HÀNH DC HƯNG YÊN", 24);
        TextView subtitle = text("PDA business client · BETA", 18);
        TextView contract = text("Domain: " + DOMAIN_CONTRACT + "\nSlice: " + SLICE, 14);
        TextView state = text("Product foundation: auth/domain screens will use the same Cloud/LAN Service contract.", 16);

        root.addView(title, matchWrap());
        root.addView(subtitle, matchWrap());
        root.addView(contract, matchWrap());
        root.addView(state, matchWrap());

        setContentView(root);
    }

    private TextView text(String value, float sizeSp) {
        TextView view = new TextView(this);
        view.setText(value);
        view.setTextSize(sizeSp);
        view.setPadding(0, dp(12), 0, dp(12));
        return view;
    }

    private LinearLayout.LayoutParams matchWrap() {
        return new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MATCH_PARENT,
            ViewGroup.LayoutParams.WRAP_CONTENT
        );
    }

    private int dp(int value) {
        return Math.round(value * getResources().getDisplayMetrics().density);
    }
}
