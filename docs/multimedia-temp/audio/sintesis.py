"""Herramientas de síntesis para el audio de Primordia: osciladores, envolventes,
filtros, reverberación y escritura de archivos. Todo trabaja con arrays de numpy
mono a 44 100 Hz (la música usa dos canales)."""

import wave

import numpy as np
from scipy import signal

SR = 44100
PEAK = 10 ** (-1 / 20)  # −1 dBFS


# ------------------------------------------------------------- señal con relleno

class Sig(np.ndarray):
    """Array mono que, al sumarse o restarse con otro más corto, rellena con silencio."""

    def _pair(self, o):
        a = np.asarray(self)
        if isinstance(o, np.ndarray) and o.ndim == 1 and a.ndim == 1 and len(o) != len(a):
            N = max(len(a), len(o))
            return np.pad(a, (0, N - len(a))), np.pad(np.asarray(o), (0, N - len(o)))
        return a, o

    def __add__(self, o):
        a, b = self._pair(o)
        return np.add(a, b).view(Sig)

    def __radd__(self, o):
        a, b = self._pair(o)
        return np.add(b, a).view(Sig)

    def __sub__(self, o):
        a, b = self._pair(o)
        return np.subtract(a, b).view(Sig)

    def __rsub__(self, o):
        a, b = self._pair(o)
        return np.subtract(b, a).view(Sig)


def padded(fn):
    """Envuelve un generador para que devuelva Sig."""
    def wrapper(*a, **k):
        r = fn(*a, **k)
        return np.asarray(r).view(Sig) if isinstance(r, np.ndarray) and np.ndim(r) == 1 else r
    wrapper.__name__ = fn.__name__
    return wrapper


# ---------------------------------------------------------------- tiempo y tono

def n(d):
    return max(1, int(round(d * SR)))


def tt(d):
    return np.arange(n(d)) / SR


def midi(m):
    return 440.0 * 2 ** ((m - 69) / 12)


def glide(f0, f1, d, shape="exp"):
    """Frecuencia que va de f0 a f1 en d segundos."""
    x = np.linspace(0, 1, n(d))
    if shape == "exp":
        return f0 * (f1 / f0) ** x
    if shape == "fast":  # llega casi enseguida y se queda
        return f1 + (f0 - f1) * np.exp(-x * 8)
    return f0 + (f1 - f0) * x


def _freq(f, d):
    if np.isscalar(f):
        return np.full(n(d), float(f))
    return np.asarray(f, dtype=float)


def phase(f, d=None):
    return 2 * np.pi * np.cumsum(_freq(f, d)) / SR


def vibrato(f, d, rate=5.5, depth=0.004, delay=0.12):
    f = _freq(f, d)
    t = np.arange(len(f)) / SR
    ramp = np.clip((t - delay) / 0.15, 0, 1)
    return f * (1 + depth * ramp * np.sin(2 * np.pi * rate * t))


# ------------------------------------------------------------------ osciladores

def sine(f, d=None):
    return np.sin(phase(f, d))


def square(f, d=None, duty=0.5):
    p = (phase(f, d) / (2 * np.pi)) % 1
    return np.where(p < duty, 1.0, -1.0)


def tri(f, d=None):
    p = (phase(f, d) / (2 * np.pi)) % 1
    return 4 * np.abs(p - 0.5) - 1


def saw(f, d=None):
    p = (phase(f, d) / (2 * np.pi)) % 1
    return 2 * p - 1


def noise(d, rng):
    return rng.uniform(-1, 1, n(d))


def fm(f, d, ratio=3.5, index=2.0, tau=0.3, itau=None):
    """Campana FM: el índice cae con el tiempo (el timbre se apaga)."""
    t = tt(d)
    itau = itau or tau * 0.6
    mod = index * np.exp(-t / itau) * np.sin(phase(np.asarray(_freq(f, d)) * ratio))
    return np.sin(phase(f, d) + mod) * np.exp(-t / tau)


# ------------------------------------------------------------------ envolventes

def expenv(d, tau, attack=0.002):
    t = tt(d)
    a = np.clip(t / attack, 0, 1) if attack > 0 else 1
    return a * np.exp(-t / tau)


def adsr(d, a=0.005, dec=0.05, s=0.7, r=0.05):
    """Envolvente de d segundos en total (la caída r va dentro)."""
    N = n(d)
    t = np.arange(N) / SR
    env = np.where(t < a, t / max(a, 1e-6),
                   np.where(t < a + dec, 1 - (1 - s) * (t - a) / max(dec, 1e-6), s))
    if r > 0:
        env = env * np.clip((d - t) / r, 0, 1)
    return env


def swell(d, peak=0.7, power=2.0):
    """Sube hasta 'peak' (fracción de d) y baja."""
    x = np.linspace(0, 1, n(d))
    up = (x / peak) ** power
    down = ((1 - x) / (1 - peak)) ** 1.2
    return np.where(x < peak, up, down)


def fade(x, fin=0.0, fout=0.0):
    x = x.copy()
    a, b = int(fin * SR), int(fout * SR)
    if a > 0:
        x[:a] *= np.linspace(0, 1, a)
    if b > 0:
        x[-b:] *= np.linspace(1, 0, b)
    return x


# -------------------------------------------------------------------- filtros

def _sos(kind, fc, order):
    fc = np.atleast_1d(fc) / (SR / 2)
    fc = np.clip(fc, 1e-4, 0.999)
    return signal.butter(order, fc if len(fc) > 1 else fc[0], kind, output="sos")


def lp(x, fc, order=2):
    return signal.sosfilt(_sos("lowpass", fc, order), x)


def hp(x, fc, order=2):
    return signal.sosfilt(_sos("highpass", fc, order), x)


def bp(x, lo, hi, order=2):
    return signal.sosfilt(_sos("bandpass", [lo, hi], order), x)


def svf(x, fc, q=1.0, mode="lp"):
    """Filtro de estado variable con frecuencia de corte que cambia en el tiempo."""
    fc = _freq(fc, len(x) / SR)[: len(x)]
    f = 2 * np.sin(np.pi * np.clip(fc, 20, SR / 6) / SR)
    damp = 1.0 / q
    low = band = 0.0
    out = np.empty(len(x))
    for i in range(len(x)):
        low += f[i] * band
        high = x[i] - low - damp * band
        band += f[i] * high
        out[i] = low if mode == "lp" else band if mode == "bp" else high
    return out


def noise_sweep(d, f0, f1, rng, q=2.0, shape="exp"):
    """Ruido por un filtro de banda que se desplaza: soplos, succiones."""
    return svf(noise(d, rng), glide(f0, f1, d, shape), q, "bp")


# ------------------------------------------------------------------- efectos

def dist(x, drive=3.0):
    return np.tanh(drive * x) / np.tanh(drive)


def crush(x, bits=6):
    s = 2 ** (bits - 1)
    return np.round(x * s) / s


def impulse_response(rt, rng, d=None, bright=4000):
    d = d or rt * 1.2
    t = tt(d)
    ir = rng.standard_normal(len(t)) * np.exp(-6.9 * t / rt)
    ir = lp(ir, bright)
    ir[0] = 0
    return ir / np.sqrt(np.sum(ir ** 2))


def reverb(x, rng, rt=0.8, wet=0.25, bright=4000, tail=True):
    ir = impulse_response(rt, rng, bright=bright)
    if tail:
        x = np.concatenate([x, np.zeros(len(ir))])
    w = signal.fftconvolve(x, ir)[: len(x)]
    return x * (1 - wet * 0.5) + w * wet


def circular_conv(x, ir):
    """Convolución circular: la cola del final cae al principio (bucles sin empalme)."""
    N = len(x)
    if len(ir) > N:
        ir = ir[:N]
    X = np.fft.rfft(x, N)
    H = np.fft.rfft(np.pad(ir, (0, N - len(ir))), N)
    return np.fft.irfft(X * H, N)


def delay(x, time, fb=0.35, mix=0.35, repeats=4, tail=True):
    D = n(time)
    if tail:
        x = np.concatenate([x, np.zeros(D * repeats)])
    out = x.copy()
    for k in range(1, repeats + 1):
        if k * D < len(x):
            out[k * D:] += x[: len(x) - k * D] * mix * fb ** (k - 1)
    return out


def am(x, rate, depth=0.5, phase0=0.0):
    t = np.arange(len(x)) / SR
    return x * (1 - depth + depth * (0.5 + 0.5 * np.sin(2 * np.pi * rate * t + phase0)))


# ------------------------------------------------------------- montaje

def place(buf, x, t, gain=1.0, wrap=False):
    """Suma x en buf a partir de t segundos (con wrap, lo que sobra vuelve al principio)."""
    i = int(round(t * SR))
    N = len(buf)
    if wrap:
        idx = (np.arange(len(x)) + i) % N
        np.add.at(buf, idx, x * gain)
        return buf
    if i >= N:
        return buf
    end = min(N, i + len(x))
    buf[i:end] += x[: end - i] * gain
    return buf


def mix(d, *parts):
    """parts = (t, señal[, ganancia]); el buffer crece si algo se sale."""
    total = max([d] + [p[0] + len(p[1]) / SR for p in parts])
    buf = np.zeros(n(total))
    for p in parts:
        place(buf, p[1], p[0], p[2] if len(p) > 2 else 1.0)
    return buf


def fit(x, d):
    N = n(d)
    if len(x) >= N:
        return x[:N]
    return np.pad(x, (0, N - len(x)))


def bubble(f, d, rise=1.7, tau=None, attack=0.003):
    """Burbuja: un seno que sube de tono mientras se apaga (resonancia de Minnaert)."""
    tau = tau or d / 3
    return sine(glide(f, f * rise, d)) * expenv(d, tau, attack)


def bubbles(d, rng, count, f_lo, f_hi, dur=(0.04, 0.1), rise=1.7, gains=(0.4, 1.0), wrap=False):
    buf = np.zeros(n(d))
    for _ in range(count):
        bd = rng.uniform(*dur)
        f = np.exp(rng.uniform(np.log(f_lo), np.log(f_hi)))
        t0 = rng.uniform(0, d if wrap else max(d - bd, 0.001))
        place(buf, bubble(f, bd, rise), t0, rng.uniform(*gains), wrap)
    return buf


def thump(f0=150, f1=45, d=0.25, tau=0.08):
    return sine(glide(f0, f1, d, "fast")) * expenv(d, tau, 0.001)


def click(rng, d=0.006, fc=3000):
    return hp(noise(d, rng), fc) * expenv(d, d / 3, 0.0005)


# --------------------------------------------------------------- salida

def normalize(x, peak=PEAK):
    m = np.max(np.abs(x))
    return x * (peak / m) if m > 0 else x


def finish_sfx(x, loop=False):
    x = np.nan_to_num(np.asarray(x, dtype=float))
    if loop:
        x = x - np.mean(x)
        return normalize(x)
    x = hp(x, 25, 1)
    m = np.max(np.abs(x))
    if m > 0:
        first = np.argmax(np.abs(x) > m * 0.003)
        x = x[first:]
    x = fade(x, fin=0.0005, fout=0.005)
    return normalize(x)


def soft_limit(x, ceiling=PEAK, knee=0.75):
    y = x.copy()
    t = ceiling * knee
    a = np.abs(y)
    over = a > t
    y[over] = np.sign(y[over]) * (t + (ceiling - t) * np.tanh((a[over] - t) / (ceiling - t)))
    return y


def rms_db(x):
    return 20 * np.log10(np.sqrt(np.mean(np.square(x))) + 1e-12)


def write_wav(path, x):
    x = np.asarray(x)
    data = np.clip(x, -1, 1)
    ch = 1 if data.ndim == 1 else data.shape[0]
    pcm = (data.T if ch > 1 else data) * 32767
    with wave.open(str(path), "wb") as w:
        w.setnchannels(ch)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(np.round(pcm).astype("<i2").tobytes())
