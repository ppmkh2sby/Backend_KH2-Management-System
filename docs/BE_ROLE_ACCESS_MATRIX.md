# Backend role access matrix

| Role | Santri and data reads | Presensi/Kafarah writes | Log writes |
| --- | --- | --- | --- |
| Admin | All records | Denied by existing KTB write policy | Existing staff read only |
| Pengurus | All records | Denied by existing KTB write policy | Existing staff read only |
| DewanGuru | All records | Denied by existing KTB write policy | Existing staff read only |
| Santri | Own records; KTB can monitor according to existing routes | KTB only | Own records only |
| WaliSantri | Only code-valid linked child selected explicitly | Denied | Denied |

An invalid/missing relation returns no child selector item and never causes a guessed relation. Wali list reads without a selected child are rejected for endpoints that require a single child, preventing first-child or all-child leakage.
