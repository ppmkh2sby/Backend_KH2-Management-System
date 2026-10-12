import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

const baseUrl = __ENV.BASE_URL;
const deviceOneId = __ENV.DEVICE_ONE_ID;
const deviceOneKey = __ENV.DEVICE_ONE_API_KEY;
const deviceTwoId = __ENV.DEVICE_TWO_ID;
const deviceTwoKey = __ENV.DEVICE_TWO_API_KEY;
const sesiId = __ENV.SESI_ID;
const image = __ENV.ATTENDANCE_IMAGE;

if (!baseUrl || !deviceOneId || !deviceOneKey || !deviceTwoId || !deviceTwoKey || !sesiId || !image) {
  throw new Error('Set BASE_URL, DEVICE_ONE_ID, DEVICE_ONE_API_KEY, DEVICE_TWO_ID, DEVICE_TWO_API_KEY, SESI_ID, and ATTENDANCE_IMAGE.');
}

export const options = {
  scenarios: {
    normal_web_traffic: {
      executor: 'ramping-vus',
      stages: [
        { duration: '2m', target: 25 },
        { duration: '3m', target: 50 },
        { duration: '3m', target: 100 },
        { duration: '2m', target: 150 },
        { duration: '1m', target: 0 },
      ],
      exec: 'health',
    },
    two_device_burst: {
      executor: 'constant-arrival-rate',
      rate: 2,
      timeUnit: '1s',
      duration: '5m',
      preAllocatedVUs: 4,
      maxVUs: 10,
      exec: 'faceAttendance',
    },
  },
  thresholds: {
    'http_req_failed{scenario:normal_web_traffic}': ['rate<0.01'],
    'http_req_failed{scenario:two_device_burst}': ['rate<0.05'],
    'face_attendance_latency': ['p(95)<5000'],
  },
};

const faceAttendanceLatency = new Trend('face_attendance_latency');
const faceAttendanceFailure = new Rate('face_attendance_failure');

export function health() {
  const response = http.get(`${baseUrl}/health/live`);
  check(response, { 'liveness is healthy': (item) => item.status === 200 });
  sleep(1);
}

export function faceAttendance() {
  const deviceOne = __VU % 2 === 0;
  const response = http.post(`${baseUrl}/api/attendance/face-recognition`, {
    SesiId: sesiId,
    Image: http.file(open(image, 'b'), 'attendance.jpg', 'image/jpeg'),
  }, {
    headers: {
      'X-Attendance-Device-Id': deviceOne ? deviceOneId : deviceTwoId,
      'X-Attendance-Device-Key': deviceOne ? deviceOneKey : deviceTwoKey,
    },
  });
  faceAttendanceLatency.add(response.timings.duration);
  faceAttendanceFailure.add(response.status >= 500);
  check(response, { 'attendance response is controlled': (item) => [200, 201, 409].includes(item.status) });
}
