# KayCare LIS/RIS Roadmap & Suggestions

This roadmap details the planned enhancements for both the Laboratory Information System (LIS) and Radiology Information System (RIS) components of KayCare LIS.

---

## 📋 1. Unified Clinical Dashboard
Integrate Radiology metrics and enhance dashboard visuals to provide a complete overview of facility operations.

- [ ] **1.1. Add Radiology Stats**: 
  - Add API endpoints or methods to retrieve counts for:
    - Scheduled scans (`RadiologyOrderStatus.Scheduled`)
    - Scans acquired (`RadiologyOrderItemStatus.Acquired`)
    - Pending reports (`RadiologyOrderItemStatus.Reported`)
  - Update [DashboardPage.tsx](file:///C:/Users/HP/Desktop/LIS/frontend/src/pages/DashboardPage.tsx) to fetch and display these statistics alongside Lab metrics.
- [ ] **1.2. SVG Dashboard Charts**:
  - Implement a lightweight, dependency-free SVG chart component to display daily volume trends for Lab Orders vs. Radiology Orders.
- [ ] **1.3. Aesthetic Polish**:
  - Apply clean Tailwind CSS gradients (e.g. `bg-gradient-to-br from-sky-50 to-indigo-50`).
  - Add hover animations (`hover:scale-[1.02] transition-transform`) to stat cards.
  - Add pulsating indicator dots for high-priority (STAT) items.

---

## 🖥️ 2. Integrated PACS / DICOM Viewer Modal
Provide an interactive medical image viewing experience directly within the app rather than linking externally.

- [ ] **2.1. PACS Modal Component**:
  - Create a new component `PacsViewerModal.tsx` in [frontend/src/components](file:///C:/Users/HP/Desktop/LIS/frontend/src/components).
  - Open this modal on clicking "View in PACS" in [RadiologyOrderDetailPage.tsx](file:///C:/Users/HP/Desktop/LIS/frontend/src/pages/radiology/RadiologyOrderDetailPage.tsx).
- [ ] **2.2. Interactive Image Manipulation**:
  - Use HTML5 `<canvas>` to display medical images.
  - Implement interactive controls:
    - **Zoom & Pan**: Drag to pan, scroll to zoom.
    - **Windowing (Brightness & Contrast)**: Adjust window width/level (or brightness/contrast CSS filters) via sliders.
    - **Measurements**: Click and drag to draw a pixel-to-millimeter line measurement overlay.
- [ ] **2.3. Realistic Mock Scans**:
  - Set up a collection of realistic mock scans (e.g., Chest X-rays, Brain MRI slices, Abdomen CT) that render depending on the selected procedure modality.

---

## 🧪 3. HL7 Simulation & Testing Suite
Simplify local testing and demonstration of MLLP/HL7 automated result entry.

- [ ] **3.1. Developer/Simulator Endpoint**:
  - Create a test controller endpoint `/api/dev/hl7/send` that accepts a JSON description of lab results.
  - Formulate this into a standard HL7 ORU^R01 message string and feed it directly into [Hl7Parser.cs](file:///C:/Users/HP/Desktop/LIS/src/KayCareLIS.Infrastructure/Services/Hl7Parser.cs) or send it over TCP to [MllpListenerService.cs](file:///C:/Users/HP/Desktop/LIS/src/KayCareLIS.Infrastructure/Services/MllpListenerService.cs).
- [ ] **3.2. HL7 Simulator Frontend Panel**:
  - Add a "HL7 Simulator" tab or floating action button on [Hl7InboxPage.tsx](file:///C:/Users/HP/Desktop/LIS/frontend/src/pages/labOrders/Hl7InboxPage.tsx).
  - Include presets for common panels (e.g., CBC, Lipid Panel, Basic Metabolic Panel) with pre-filled test codes.
  - Allow editing values and clicking "Transmit Message" to watch the results populate instantly.

---

## 📅 4. Patient EHR Clinical Timeline
Merge all separate patient records into a unified chronological medical history view.

- [ ] **4.1. Core Timeline View**:
  - Add a timeline view tab in the patient details page [PatientDetailPage.tsx](file:///C:/Users/HP/Desktop/LIS/frontend/src/pages/patients/PatientDetailPage.tsx).
  - Collate Appointments, Bills, Lab Orders, and Radiology Orders.
  - Sort chronologically to show a clear history of the patient's care.
- [ ] **4.2. Diagnostic Previews**:
  - Allow expanding timeline nodes to preview signed lab result tables or radiology impression text directly without navigating away.

---

## ✨ 5. UI/UX Refinement
Elevate the visual feedback and interactive feel of the application.

- [ ] **5.1. Global Font and Styling Tokens**:
  - Load a clean, modern geometric sans-serif typeface (like Outfit or Inter) in [index.html](file:///C:/Users/HP/Desktop/LIS/frontend/index.html).
- [ ] **5.2. Page Transitions**:
  - Implement smooth page entry transitions (e.g., fade-in/slide-up animations) when switching routes.
- [ ] **5.3. Scrollbar & Border styling**:
  - Style custom scrollbars for sidebars and tables.
  - Use subtle border colors and interactive outlines on input focus.
