import type { Metadata } from "next";
import { AuthProvider } from "@/components/admin/auth-provider";
import { Toaster } from "@/components/ui/toaster";

export const metadata: Metadata = {
  title: { default: "Backoffice", template: "%s · Backoffice Zoi" },
  robots: { index: false, follow: false, nocache: true },
};

// Everything under /admin depends on the session in the browser: never prerender data.
export const dynamic = "force-dynamic";

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return (
    <AuthProvider>
      {children}
      <Toaster />
    </AuthProvider>
  );
}
