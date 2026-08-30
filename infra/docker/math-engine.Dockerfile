# syntax=docker/dockerfile:1

FROM python:3.12-slim AS build
WORKDIR /app
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1

COPY pyproject.toml ./
COPY app ./app
RUN pip install --no-cache-dir --prefix=/install .

FROM python:3.12-slim AS final
WORKDIR /app
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1
COPY --from=build /install /usr/local
COPY app ./app
EXPOSE 8000
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
